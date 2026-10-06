using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class RejectApplicationRequestValidator : AbstractValidator<RejectApplicationRequest>
{
    public RejectApplicationRequestValidator()
    {
        RuleFor(request => request.Comment)
            .NotEmpty()
            .WithMessage("Укажите комментарий к отказу.")
            .MaximumLength(1000)
            .WithMessage("Укажите комментарий к отказу длиной не более 1000 символов.");
    }
}
