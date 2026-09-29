using CreditCalculator.Domain.Enums;

namespace CreditCalculator.Domain.Entities;

public class ApplicationStatusHistory
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }

    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }

    public Guid? ChangedByUserId { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public Application Application { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
