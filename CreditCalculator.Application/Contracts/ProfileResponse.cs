using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record ProfileResponse(
    string FullName,
    DateOnly BirthDate,
    Gender? Gender,
    decimal MonthlyIncome,
    int EmploymentMonths,
    decimal ExistingMonthlyPayments,
    int Dependents,
    DateTimeOffset UpdatedAt);
