using System.Net;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Exceptions;
using CreditCalculator.Application.Scoring;
using CreditCalculator.Calculations.Schedules;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Services;

public sealed class ApplicationService : IApplicationService
{
    private readonly IAppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly IScoringService _scoringService;

    public ApplicationService(IAppDbContext dbContext, TimeProvider timeProvider, IScoringService scoringService)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _scoringService = scoringService;
    }

    // Ставка фиксируется из справочника продукта, а не из запроса клиента:
    // клиент не может подать заявку по ставке, которая в продукте не действует.
    public async Task<CreateApplicationResult> CreateAsync(
        Guid userId,
        CreateApplicationRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (key is not null && key.Length > CreateApplicationIdempotency.MaxKeyLength)
        {
            throw new BusinessRuleException(
                $"Заголовок Idempotency-Key не должен быть длиннее {CreateApplicationIdempotency.MaxKeyLength} символов.");
        }

        var requestHash = key is null ? null : CreateApplicationIdempotency.ComputeRequestHash(request);

        if (key is not null)
        {
            var existing = await _dbContext.IdempotencyKeys
                .AsNoTracking()
                .Include(idempotencyKey => idempotencyKey.Application.StatusHistory)
                .Include(idempotencyKey => idempotencyKey.Application.CreditProduct)
                .FirstOrDefaultAsync(idempotencyKey => idempotencyKey.Key == key, cancellationToken);
            if (existing is not null)
            {
                if (existing.UserId != userId)
                {
                    // Ответ на чужой ключ такой же, как на несуществующий: по ключу нельзя разведать чужие заявки.
                    throw new NotFoundException("Заявка не найдена.");
                }

                EnsureMatchingRequestBody(existing, requestHash!);
                return Replay(existing.Application);
            }
        }

        var product = await _dbContext.CreditProducts
            .FirstOrDefaultAsync(product => product.Id == request.CreditProductId && product.IsActive, cancellationToken)
            ?? throw new NotFoundException("Кредитный продукт не найден.");

        if (request.Amount < product.MinAmount || request.Amount > product.MaxAmount)
        {
            throw new BusinessRuleException(
                $"Сумма заявки для продукта «{product.Name}» должна быть от {product.MinAmount} до {product.MaxAmount} BYN.");
        }

        if (request.TermMonths < product.MinTermMonths || request.TermMonths > product.MaxTermMonths)
        {
            throw new BusinessRuleException(
                $"Срок заявки для продукта «{product.Name}» должен быть от {product.MinTermMonths} до {product.MaxTermMonths} месяцев.");
        }

        if (product.Purpose == CreditPurpose.Mortgage && request.DownPaymentAmount is null)
        {
            throw new BusinessRuleException("Для ипотечной заявки укажите первоначальный взнос.");
        }

        if (product.Purpose != CreditPurpose.Mortgage && request.DownPaymentAmount is not null)
        {
            throw new BusinessRuleException("Первоначальный взнос указывается только для ипотечной заявки.");
        }

        var profile = await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Для подачи заявки сначала заполните анкету.");

        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.Date);
        var application = new ApplicationEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CreditProductId = product.Id,
            Amount = request.Amount,
            TermMonths = request.TermMonths,
            InterestRate = product.BaseRate,
            CreditProduct = product,
            CreatedAt = now,
            UpdatedAt = now
        };
        application.ApplySnapshot(
            profile.MonthlyIncome,
            profile.ExistingMonthlyPayments,
            profile.GetAgeOn(today),
            profile.BirthDate,
            profile.Gender,
            profile.EmploymentMonths,
            profile.Dependents,
            request.DownPaymentAmount);

        application.ChangeStatus(ApplicationStatus.Submitted, userId);
        application.ChangeStatus(ApplicationStatus.Scoring, userId);

        var evaluation = _scoringService.Evaluate(application);
        application.Score = evaluation.Score;
        foreach (var result in evaluation.RuleResults)
        {
            application.ScoringResults.Add(new ScoringResult
            {
                Id = Guid.CreateVersion7(),
                ApplicationId = application.Id,
                RuleCode = result.RuleCode,
                RuleName = result.RuleName,
                Passed = result.Passed,
                Points = result.Points,
                Details = result.Details
            });
        }

        application.ChangeStatus(evaluation.Decision switch
        {
            ScoringDecision.AutoApproved => ApplicationStatus.AutoApproved,
            ScoringDecision.UnderReview => ApplicationStatus.UnderReview,
            ScoringDecision.Rejected => ApplicationStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(evaluation), evaluation.Decision, "Неизвестное решение скоринга.")
        }, userId);

        _dbContext.Applications.Add(application);
        if (key is not null)
        {
            _dbContext.IdempotencyKeys.Add(new IdempotencyKey
            {
                Key = key,
                UserId = userId,
                ApplicationId = application.Id,
                RequestBodyHash = requestHash!,
                CreatedAt = now
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (key is null)
                throw;

            // Параллельный запрос с тем же Idempotency-Key успел вставить заявку первым (гонка упёрлась в уникальный ключ):
            // вернём уже созданную заявку, а не дубль и не ошибку.
            var winner = await _dbContext.IdempotencyKeys
                .AsNoTracking()
                .Include(idempotencyKey => idempotencyKey.Application.StatusHistory)
                .Include(idempotencyKey => idempotencyKey.Application.CreditProduct)
                .FirstOrDefaultAsync(idempotencyKey => idempotencyKey.Key == key, cancellationToken);
            if (winner is not null && winner.UserId == userId)
            {
                EnsureMatchingRequestBody(winner, requestHash!);
                return Replay(winner.Application);
            }

            throw;
        }

        return Created(ToResponse(application, product.Name));
    }

    // Только заявки текущего пользователя, новые — выше.
    public async Task<PagedResponse<ApplicationResponse>> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Applications
            .AsNoTracking()
            .Include(application => application.StatusHistory)
            .Include(application => application.CreditProduct)
            .Where(application => application.UserId == userId)
            .OrderByDescending(application => application.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var applications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ApplicationResponse>(
            page,
            pageSize,
            totalCount,
            applications.Select(application => ToResponse(application)).ToList());
    }

    public async Task<PagedResponse<EmployeeApplicationListItemResponse>> GetEmployeePagedAsync(
        EmployeeApplicationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var applications = _dbContext.Applications
            .AsNoTracking()
            .Include(application => application.CreditProduct)
            .AsQueryable();

        if (query.MinAmount.HasValue)
            applications = applications.Where(application => application.Amount >= query.MinAmount.Value);
        if (query.MaxAmount.HasValue)
            applications = applications.Where(application => application.Amount <= query.MaxAmount.Value);
        if (query.CreatedFrom.HasValue)
            applications = applications.Where(application => application.CreatedAt >= query.CreatedFrom.Value);
        if (query.CreatedTo.HasValue)
            applications = applications.Where(application => application.CreatedAt <= query.CreatedTo.Value);
        if (query.MinScore.HasValue)
            applications = applications.Where(application => application.Score.HasValue && application.Score.Value >= query.MinScore.Value);
        if (query.MaxScore.HasValue)
            applications = applications.Where(application => application.Score.HasValue && application.Score.Value <= query.MaxScore.Value);
        if (query.Status.HasValue)
            applications = applications.Where(application => application.Status == query.Status.Value);

        var totalCount = await applications.CountAsync(cancellationToken);
        var orderedApplications = (query.SortBy switch
        {
            EmployeeApplicationSortField.Amount => query.Descending
                ? applications.OrderByDescending(application => application.Amount)
                : applications.OrderBy(application => application.Amount),
            EmployeeApplicationSortField.Score => query.Descending
                ? applications.OrderByDescending(application => application.Score)
                : applications.OrderBy(application => application.Score),
            EmployeeApplicationSortField.Status => query.Descending
                ? applications.OrderByDescending(application => application.Status)
                : applications.OrderBy(application => application.Status),
            _ => query.Descending
                ? applications.OrderByDescending(application => application.CreatedAt)
                : applications.OrderBy(application => application.CreatedAt)
        }).ThenBy(application => application.Id);

        var items = await orderedApplications
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(application => new EmployeeApplicationListItemResponse(
                application.Id,
                application.CreditProductId,
                application.CreditProduct.Name,
                application.Amount,
                application.TermMonths,
                application.InterestRate,
                application.Status,
                application.Score,
                application.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<EmployeeApplicationListItemResponse>(
            query.Page,
            query.PageSize,
            totalCount,
            items);
    }

    public async Task<EmployeeApplicationDetailsResponse> GetEmployeeByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .AsNoTracking()
            .Include(item => item.StatusHistory)
            .Include(item => item.CreditProduct)
            .Include(item => item.ScoringResults)
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken)
            ?? throw new NotFoundException("Заявка не найдена.");

        var firstPaymentDate = DateOnly.FromDateTime(application.CreatedAt.UtcDateTime).AddMonths(1);
        var schedule = AnnuityScheduleCalculator.BuildSchedule(
            application.Amount,
            application.InterestRate,
            application.TermMonths,
            firstPaymentDate);

        return new EmployeeApplicationDetailsResponse(
            ToResponse(application),
            application.ScoringResults
                .OrderBy(result => result.RuleCode)
                .Select(result => new EmployeeScoringResultResponse(
                    result.RuleCode,
                    result.RuleName,
                    result.Passed,
                    result.Points,
                    result.Details))
                .ToList(),
            new ProposedPaymentScheduleResponse(firstPaymentDate, schedule));
    }

    public async Task<ApplicationResponse> GetByIdAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .AsNoTracking()
            .Include(application => application.StatusHistory)
            .Include(application => application.CreditProduct)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("Заявка не найдена.");

        return ToResponse(application);
    }

    public async Task<ApplicationResponse> WithdrawAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .Include(application => application.StatusHistory)
            .Include(application => application.CreditProduct)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.UserId == userId,
                cancellationToken)
            ?? throw new NotFoundException("Заявка не найдена.");

        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Submitted or ApplicationStatus.Scoring or ApplicationStatus.UnderReview))
        {
            throw new BusinessRuleException("Заявку нельзя отозвать после принятия решения.", HttpStatusCode.Conflict);
        }

        application.ChangeStatus(ApplicationStatus.Withdrawn, userId);
        _dbContext.ApplicationStatusHistories.Add(application.StatusHistory[^1]);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(application);
    }

    private static void EnsureMatchingRequestBody(IdempotencyKey existing, string requestHash)
    {
        if (!string.Equals(existing.RequestBodyHash, requestHash, StringComparison.Ordinal))
        {
            throw new BusinessRuleException(
                "Idempotency-Key уже использован с другими параметрами заявки.",
                HttpStatusCode.Conflict);
        }
    }

    private static CreateApplicationResult Created(ApplicationResponse application) =>
        new(application, WasReplayed: false);

    private static CreateApplicationResult Replay(ApplicationEntity application) =>
        new(ToResponse(application), WasReplayed: true);

    private static ApplicationResponse ToResponse(ApplicationEntity application, string? productName = null) => new(
        application.Id,
        application.CreditProductId,
        application.CreditProduct?.Name ?? productName ?? string.Empty,
        application.Amount,
        application.TermMonths,
        application.InterestRate,
        application.Status,
        application.Score,
        application.IncomeAtApply,
        application.ExistingPaymentsAtApply,
        application.AgeAtApply,
        application.CreatedAt,
        application.UpdatedAt,
        application.StatusHistory
            .OrderBy(entry => entry.ChangedAt)
            .Select(entry => new ApplicationStatusHistoryResponse(
                entry.FromStatus,
                entry.ToStatus,
                entry.ChangedAt,
                entry.Comment))
            .ToList());
}
