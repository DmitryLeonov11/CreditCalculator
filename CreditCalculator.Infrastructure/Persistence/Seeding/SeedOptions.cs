namespace CreditCalculator.Infrastructure.Persistence.Seeding;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string EmployeeEmail { get; set; } = string.Empty;
    public string EmployeePassword { get; set; } = string.Empty;
}
