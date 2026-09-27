using CreditCalculator.Api.RateLimiting;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingServiceCollectionExtensions.AuthPolicy)]
    public async Task<ActionResult<RegisteredUserResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var registeredUser = await _authService.RegisterAsync(
            request,
            token => Url.ActionLink(nameof(ConfirmEmail), values: new { token })!,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, registeredUser);
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
