using CreditCalculator.Application.Contracts;

namespace CreditCalculator.Application.Services;

public interface IAuthService
{
    Task RegisterAsync(
        RegisterRequest request,
        Func<string, string> buildConfirmationLink,
        CancellationToken cancellationToken = default);

    Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);

    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
