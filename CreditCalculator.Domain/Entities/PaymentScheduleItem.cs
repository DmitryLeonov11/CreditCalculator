namespace CreditCalculator.Domain.Entities;

public class PaymentScheduleItem
{
    public Guid Id { get; set; }
    public Guid PaymentScheduleId { get; set; }
    public int Number { get; set; }
    public DateOnly Date { get; set; }
    public decimal Payment { get; set; }
    public decimal InterestPart { get; set; }
    public decimal PrincipalPart { get; set; }
    public decimal RemainingBalance { get; set; }

    public PaymentSchedule PaymentSchedule { get; set; } = null!;
}
