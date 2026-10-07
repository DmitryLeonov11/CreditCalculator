using CreditCalculator.Api.Authentication;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/employee/statistics")]
[Authorize(Policy = AuthorizationPolicies.EmployeeOnly)]
public sealed class EmployeeStatisticsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public EmployeeStatisticsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeStatisticsResponse>> Get(
        [FromQuery] GetEmployeeStatisticsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetEmployeeStatisticsAsync(query.From, query.To, cancellationToken));
}
