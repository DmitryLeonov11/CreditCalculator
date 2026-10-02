using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Exceptions;
using CreditCalculator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditCalculator.Application.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IAppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ProfileService(IAppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Анкета ещё не заполнена.");

        return ToResponse(profile);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new Profile { UserId = userId };
            _dbContext.Profiles.Add(profile);
        }

        profile.FullName = request.FullName.Trim();
        profile.BirthDate = request.BirthDate;
        profile.Gender = request.Gender;
        profile.MonthlyIncome = request.MonthlyIncome;
        profile.EmploymentMonths = request.EmploymentMonths;
        profile.ExistingMonthlyPayments = request.ExistingMonthlyPayments;
        profile.Dependents = request.Dependents;
        profile.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(profile);
    }

    private static ProfileResponse ToResponse(Profile profile) => new(
        profile.FullName,
        profile.BirthDate,
        profile.Gender,
        profile.MonthlyIncome,
        profile.EmploymentMonths,
        profile.ExistingMonthlyPayments,
        profile.Dependents,
        profile.UpdatedAt);
}
