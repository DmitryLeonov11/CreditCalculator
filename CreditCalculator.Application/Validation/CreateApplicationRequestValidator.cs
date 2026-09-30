using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(request => request.CreditProductId)
            .NotEmpty()
            .WithMessage("Укажите кредитный продукт.");

        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage("Сумма должна быть больше нуля.");

        // Верхняя граница совпадает с check-констрейном таблицы Applications;
        // конкретные границы продукта проверяет сервис при создании заявки.
        RuleFor(request => request.TermMonths)
            .InclusiveBetween(1, 360)
            .WithMessage("Срок должен быть от 1 до 360 месяцев.");
    }
}
