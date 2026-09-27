using CreditCalculator.Application.Contracts;
using FluentValidation;

namespace CreditCalculator.Application.Validation;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .WithMessage("Укажите refresh-токен.");
    }
}
