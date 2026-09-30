using CreditCalculator.Api.Contracts;
using FluentValidation;

namespace CreditCalculator.Api.Validation;

public sealed class GetApplicationsPageQueryValidator : AbstractValidator<GetApplicationsPageQuery>
{
    public GetApplicationsPageQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер страницы должен быть не меньше 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Размер страницы должен быть от 1 до 100.");
    }
}
