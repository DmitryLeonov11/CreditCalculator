using CreditCalculator.Api.Options;
using CreditCalculator.Api.RateLimiting;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly PublicUrlOptions _publicUrlOptions;

    public AuthController(IAuthService authService, IOptions<PublicUrlOptions> publicUrlOptions)
    {
        _authService = authService;
        _publicUrlOptions = publicUrlOptions.Value;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingServiceCollectionExtensions.AuthPolicy)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Хост ссылки берём из конфигурации, а не из заголовка Host: иначе подменённый Host
        // увёл бы ссылку с токеном подтверждения на чужой домен.
        await _authService.RegisterAsync(
            request,
            token => _publicUrlOptions.BaseUrl.TrimEnd('/') + Url.Action(nameof(ConfirmEmail), new { token }),
            cancellationToken);

        return Accepted();
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token, CancellationToken cancellationToken)
    {
        await _authService.ConfirmEmailAsync(token, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingServiceCollectionExtensions.AuthPolicy)]
    public async Task<ActionResult<AuthTokensResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokensResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.RefreshAsync(request, cancellationToken));
}
