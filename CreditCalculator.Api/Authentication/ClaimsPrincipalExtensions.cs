using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CreditCalculator.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
