using CreditCalculator.Api.Contracts;
using FluentValidation;

namespace CreditCalculator.Api.Validation;

public sealed class GetEmployeeApplicationsPageQueryValidator : AbstractValidator<GetEmployeeApplicationsPageQuery>
{
    public GetEmployeeApplicationsPageQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер страницы должен быть не меньше 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Размер страницы должен быть от 1 до 100.");

        RuleFor(query => query.MinAmount)
            .GreaterThanOrEqualTo(0m)
            .When(query => query.MinAmount.HasValue);

        RuleFor(query => query.MaxAmount)
            .GreaterThan(0m)
            .When(query => query.MaxAmount.HasValue);

        RuleFor(query => query)
            .Must(query => !query.MinAmount.HasValue || !query.MaxAmount.HasValue || query.MinAmount <= query.MaxAmount)
            .WithMessage("Минимальная сумма не должна превышать максимальную.");

        RuleFor(query => query.MinScore)
            .InclusiveBetween(0, 100)
            .When(query => query.MinScore.HasValue);

        RuleFor(query => query.MaxScore)
            .InclusiveBetween(0, 100)
            .When(query => query.MaxScore.HasValue);

        RuleFor(query => query)
            .Must(query => !query.MinScore.HasValue || !query.MaxScore.HasValue || query.MinScore <= query.MaxScore)
            .WithMessage("Минимальный балл не должен превышать максимальный.");

        RuleFor(query => query)
            .Must(query => !query.CreatedFrom.HasValue || !query.CreatedTo.HasValue || query.CreatedFrom <= query.CreatedTo)
            .WithMessage("Начало периода не должно быть позже его окончания.");

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status.HasValue);

        RuleFor(query => query.SortBy)
            .IsInEnum();
    }
}
