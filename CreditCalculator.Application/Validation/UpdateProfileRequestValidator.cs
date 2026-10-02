using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .WithMessage("Укажите ФИО.")
            .MaximumLength(200)
            .WithMessage("ФИО не должно быть длиннее 200 символов.");

        RuleFor(request => request.BirthDate)
            .Must(birthDate =>
            {
                var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().Date);
                return birthDate <= today.AddYears(-18) && birthDate > today.AddYears(-100);
            })
            .WithMessage("Возраст клиента должен быть от 18 до 100 лет.");

        RuleFor(request => request.Gender)
            .IsInEnum()
            .WithMessage("Укажите пол клиента.");

        RuleFor(request => request.MonthlyIncome)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Ежемесячный доход не может быть отрицательным.");

        RuleFor(request => request.EmploymentMonths)
            .InclusiveBetween(0, 600)
            .WithMessage("Стаж на текущем месте работы должен быть от 0 до 600 месяцев.");

        RuleFor(request => request.ExistingMonthlyPayments)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Сумма текущих ежемесячных платежей не может быть отрицательной.");

        RuleFor(request => request.Dependents)
            .InclusiveBetween(0, 20)
            .WithMessage("Количество иждивенцев должно быть от 0 до 20.");
    }
}
