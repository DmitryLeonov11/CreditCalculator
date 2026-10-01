using System.Net;
using ApplicationEntity = CreditCalculator.Domain.Entities.Application;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Exceptions;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Services;

public sealed class ApplicationService : IApplicationService
{
    private readonly IAppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ApplicationService(IAppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
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
            .AsNoTracking()
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
            CreatedAt = now,
            UpdatedAt = now
        };
        application.ApplySnapshot(profile.MonthlyIncome, profile.ExistingMonthlyPayments, profile.GetAgeOn(today));

        // Заявка создаётся в Draft и сразу переходит в Submitted через конечный автомат:
        // в истории статусов с первого момента есть запись Draft → Submitted.
        application.ChangeStatus(ApplicationStatus.Submitted, userId);

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
