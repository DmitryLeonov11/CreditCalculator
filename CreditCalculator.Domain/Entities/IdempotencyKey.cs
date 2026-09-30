namespace CreditCalculator.Domain.Entities;

public class IdempotencyKey
{
    public string Key { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string RequestBodyHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Application Application { get; set; } = null!;
}
