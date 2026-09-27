namespace CreditCalculator.Application.Contracts;

public sealed record UpdateProfileRequest(
    string FullName,
    DateOnly BirthDate,
    decimal MonthlyIncome,
    int EmploymentMonths,
    decimal ExistingMonthlyPayments,
    int Dependents);
