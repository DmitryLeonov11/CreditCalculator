using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class ApproveApplicationRequestValidator : AbstractValidator<ApproveApplicationRequest>
{
    public ApproveApplicationRequestValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("Сумма должна быть больше нуля.");

        RuleFor(request => request.TermMonths)
            .InclusiveBetween(1, 360)
            .WithMessage("Срок должен быть от 1 до 360 месяцев.");

        RuleFor(request => request.InterestRate)
            .InclusiveBetween(0, 60)
            .WithMessage("Ставка должна быть от 0 до 60% годовых.");

        RuleFor(request => request.Comment)
            .MaximumLength(1000)
            .WithMessage("Комментарий не должен превышать 1000 символов.");
    }
}
