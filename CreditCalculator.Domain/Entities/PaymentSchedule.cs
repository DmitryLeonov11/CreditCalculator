namespace CreditCalculator.Domain.Entities;

public class PaymentSchedule
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public DateOnly FirstPaymentDate { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Overpayment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Application Application { get; set; } = null!;
    public List<PaymentScheduleItem> Payments { get; set; } = [];
}
