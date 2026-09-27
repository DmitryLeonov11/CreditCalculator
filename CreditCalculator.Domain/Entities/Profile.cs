namespace CreditCalculator.Domain.Entities;

public class Profile
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public decimal MonthlyIncome { get; set; }
    public int EmploymentMonths { get; set; }
    public decimal ExistingMonthlyPayments { get; set; }
    public int Dependents { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
