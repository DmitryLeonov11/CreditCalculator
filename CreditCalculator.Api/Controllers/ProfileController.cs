using CreditCalculator.Api.Authentication;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize(Policy = AuthorizationPolicies.ClientOnly)]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await _profileService.GetAsync(User.GetUserId(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await _profileService.UpdateAsync(User.GetUserId(), request, cancellationToken));
}
