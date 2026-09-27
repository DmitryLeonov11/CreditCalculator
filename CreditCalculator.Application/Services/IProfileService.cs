using CreditCalculator.Application.Contracts;

namespace CreditCalculator.Application.Services;

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileResponse> UpdateAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
