using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset PersonalDataConsentAt { get; set; }

    public Profile? Profile { get; set; }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
