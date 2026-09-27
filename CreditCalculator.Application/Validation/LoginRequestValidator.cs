using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Укажите email.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Укажите пароль.");
    }
}
