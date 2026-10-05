using CreditCalculator.Api.Authentication;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/employee/applications")]
[Authorize(Policy = AuthorizationPolicies.EmployeeOnly)]
public sealed class EmployeeApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public EmployeeApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<EmployeeApplicationListItemResponse>>> GetPage(
        [FromQuery] GetEmployeeApplicationsPageQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetEmployeePagedAsync(query.ToApplicationQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeApplicationDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetEmployeeByIdAsync(id, cancellationToken));
}
