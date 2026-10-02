using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Application.Contracts;

public sealed record UpdateProfileRequest(
    string FullName,
    DateOnly BirthDate,
    Gender Gender,
    decimal MonthlyIncome,
    int EmploymentMonths,
    decimal ExistingMonthlyPayments,
    int Dependents);
