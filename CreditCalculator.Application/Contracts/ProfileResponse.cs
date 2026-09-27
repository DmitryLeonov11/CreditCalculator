namespace CreditCalculator.Application.Contracts;

public sealed record ProfileResponse(
    string FullName,
    DateOnly BirthDate,
    decimal MonthlyIncome,
    int EmploymentMonths,
    decimal ExistingMonthlyPayments,
    int Dependents,
    DateTimeOffset UpdatedAt);
