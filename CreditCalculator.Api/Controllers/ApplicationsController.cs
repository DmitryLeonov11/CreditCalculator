using CreditCalculator.Api.Authentication;
using CreditCalculator.Api.Contracts;
using CreditCalculator.Api.Export;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/applications")]
[Authorize(Policy = AuthorizationPolicies.ClientOnly)]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateApplicationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.CreateAsync(User.GetUserId(), request, idempotencyKey, cancellationToken);
        return result.WasReplayed
            ? Ok(result.Application)
            : Created($"/api/v1/applications/{result.Application.Id}", result.Application);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ApplicationResponse>>> GetPage(
        [FromQuery] GetApplicationsPageQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetPagedAsync(User.GetUserId(), query.Page, query.PageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetByIdAsync(User.GetUserId(), id, cancellationToken));

    [HttpGet("{id:guid}/schedule")]
    public async Task<ActionResult<ApplicationScheduleResponse>> GetSchedule(Guid id, CancellationToken cancellationToken) =>
        Ok(await _applicationService.GetScheduleAsync(User.GetUserId(), id, cancellationToken));

    [HttpGet("{id:guid}/schedule/pdf")]
    public async Task<IActionResult> ExportSchedulePdf(Guid id, CancellationToken cancellationToken)
    {
        var response = await _applicationService.GetScheduleAsync(User.GetUserId(), id, cancellationToken);
        var pdf = PaymentSchedulePdfExporter.Generate("График платежей по заявке", response.Schedule);
        return File(pdf, "application/pdf", $"application-{id:N}-schedule.pdf");
    }

    [HttpGet("{id:guid}/schedule/excel")]
    public async Task<IActionResult> ExportScheduleExcel(Guid id, CancellationToken cancellationToken)
    {
        var response = await _applicationService.GetScheduleAsync(User.GetUserId(), id, cancellationToken);
        var excel = PaymentScheduleExcelExporter.Generate(response.Schedule);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"application-{id:N}-schedule.xlsx");
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<ApplicationResponse>> Withdraw(Guid id, CancellationToken cancellationToken) =>
        Ok(await _applicationService.WithdrawAsync(User.GetUserId(), id, cancellationToken));
}
