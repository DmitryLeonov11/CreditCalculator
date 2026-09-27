using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Укажите email.")
            .EmailAddress()
            .WithMessage("Некорректный формат email.")
            .MaximumLength(256)
            .WithMessage("Email не должен быть длиннее 256 символов.");

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Укажите пароль.")
            .MinimumLength(8)
            .WithMessage("Пароль должен содержать не менее 8 символов.")
            .MaximumLength(128)
            .WithMessage("Пароль не должен быть длиннее 128 символов.")
            .Must(password => password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage("Пароль должен содержать хотя бы одну букву и одну цифру.");

        RuleFor(request => request.PersonalDataConsent)
            .Equal(true)
            .WithMessage("Для регистрации необходимо согласие на обработку персональных данных.");
    }
}
