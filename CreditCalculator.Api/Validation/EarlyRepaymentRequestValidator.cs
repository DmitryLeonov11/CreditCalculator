using CreditCalculator.Api.Contracts;
using FluentValidation;

namespace CreditCalculator.Api.Validation;

public sealed class EarlyRepaymentRequestValidator : AbstractValidator<EarlyRepaymentRequest>
{
    public EarlyRepaymentRequestValidator()
    {
        RuleFor(request => request.Amount)
            .InclusiveBetween(500m, 300_000m)
            .WithMessage("Сумма кредита должна быть от 500 до 300 000 BYN.");

        RuleFor(request => request.TermMonths)
            .InclusiveBetween(1, 240)
            .WithMessage("Срок кредита должен быть от 1 до 240 месяцев.");

        RuleFor(request => request.AnnualRatePercent)
            .InclusiveBetween(0.1m, 60m)
            .WithMessage("Процентная ставка должна быть от 0,1% до 60% годовых.");

        RuleFor(request => request.Mode)
            .IsInEnum()
            .WithMessage("Неизвестный режим досрочного погашения.");

        RuleForEach(request => request.EarlyRepayments)
            .ChildRules(item => item
                .RuleFor(x => x.Amount)
                .GreaterThan(0m)
                .WithMessage("Сумма досрочного погашения должна быть больше нуля."));

        RuleForEach(request => request.EarlyRepayments)
            .Must((request, item) => item.Month >= 1 && item.Month <= request.TermMonths)
            .WithMessage(request => $"Месяц досрочного погашения должен быть от 1 до {request.TermMonths}.");
    }
}
