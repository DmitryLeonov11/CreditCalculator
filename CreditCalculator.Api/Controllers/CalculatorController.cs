using CreditCalculator.Api.Contracts;
using CreditCalculator.Api.Mapping;
using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Schedules;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CreditCalculator.Api.Controllers;

[ApiController]
[Route("api/v1/calculator")]
public sealed class CalculatorController : ControllerBase
{
    private readonly IValidator<ScheduleRequest> _scheduleRequestValidator;
    private readonly IValidator<EarlyRepaymentRequest> _earlyRepaymentRequestValidator;

    public CalculatorController(
        IValidator<ScheduleRequest> scheduleRequestValidator,
        IValidator<EarlyRepaymentRequest> earlyRepaymentRequestValidator)
    {
        _scheduleRequestValidator = scheduleRequestValidator;
        _earlyRepaymentRequestValidator = earlyRepaymentRequestValidator;
    }

    [HttpPost("schedule")]
    public ActionResult<ScheduleResponse> GetSchedule(ScheduleRequest request)
    {
        var validationResult = _scheduleRequestValidator.Validate(request);
        if (!validationResult.IsValid)
            return ValidationProblem(ToModelState(validationResult));

        var scheduleResult = request.PaymentType == PaymentType.Annuity
            ? AnnuityScheduleCalculator.BuildSchedule(request.Amount, request.AnnualRatePercent, request.TermMonths, request.FirstPaymentDate)
            : DifferentiatedScheduleCalculator.BuildSchedule(request.Amount, request.AnnualRatePercent, request.TermMonths, request.FirstPaymentDate);

        return Ok(ScheduleResponseMapper.ToResponse(request.Amount, scheduleResult));
    }

    [HttpPost("early-repayment")]
    public ActionResult<EarlyRepaymentResponse> GetEarlyRepaymentComparison(EarlyRepaymentRequest request)
    {
        var validationResult = _earlyRepaymentRequestValidator.Validate(request);
        if (!validationResult.IsValid)
            return ValidationProblem(ToModelState(validationResult));

        var earlyRepayments = request.EarlyRepayments
            .Select(item => new EarlyRepayment(item.Month, item.Amount))
            .ToList();

        var originalSchedule = AnnuityScheduleCalculator.BuildSchedule(
            request.Amount, request.AnnualRatePercent, request.TermMonths, request.FirstPaymentDate);

        var recalculatedSchedule = request.Mode == EarlyRepaymentMode.ReduceTerm
            ? EarlyRepaymentScheduleCalculator.BuildReduceTermSchedule(
                request.Amount, request.AnnualRatePercent, request.TermMonths, request.FirstPaymentDate, earlyRepayments)
            : EarlyRepaymentScheduleCalculator.BuildReducePaymentSchedule(
                request.Amount, request.AnnualRatePercent, request.TermMonths, request.FirstPaymentDate, earlyRepayments);

        var response = new EarlyRepaymentResponse(
            ScheduleResponseMapper.ToResponse(request.Amount, originalSchedule),
            ScheduleResponseMapper.ToResponse(request.Amount, recalculatedSchedule));

        return Ok(response);
    }

    private ModelStateDictionary ToModelState(FluentValidation.Results.ValidationResult validationResult)
    {
        foreach (var error in validationResult.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return ModelState;
    }
}
