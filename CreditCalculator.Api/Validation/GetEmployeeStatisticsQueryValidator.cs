using CreditCalculator.Api.Contracts;
using FluentValidation;

namespace CreditCalculator.Api.Validation;

public sealed class GetEmployeeStatisticsQueryValidator : AbstractValidator<GetEmployeeStatisticsQuery>
{
    public GetEmployeeStatisticsQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From <= query.To)
            .WithMessage("Начало периода не должно быть позже его окончания.");
    }
}
