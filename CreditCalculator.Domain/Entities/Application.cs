using CreditCalculator.Domain.Enums;
using CreditCalculator.Domain.Exceptions;

namespace CreditCalculator.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CreditProductId { get; set; }

    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public decimal InterestRate { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    // Снимок анкеты на момент подачи заявки
    public decimal? IncomeAtApply { get; set; }
    public decimal? ExistingPaymentsAtApply { get; set; }
    public int? AgeAtApply { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public CreditProduct CreditProduct { get; set; } = null!;

    public bool CanTransitionTo(ApplicationStatus targetStatus)
    {
        return (Status, targetStatus) switch
        {
            (ApplicationStatus.Draft, ApplicationStatus.Submitted) => true,
            (ApplicationStatus.Draft, ApplicationStatus.Withdrawn) => true,

            (ApplicationStatus.Submitted, ApplicationStatus.Scoring) => true,
            (ApplicationStatus.Submitted, ApplicationStatus.Withdrawn) => true,

            (ApplicationStatus.Scoring, ApplicationStatus.AutoApproved) => true,
            (ApplicationStatus.Scoring, ApplicationStatus.UnderReview) => true,
            (ApplicationStatus.Scoring, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Scoring, ApplicationStatus.Withdrawn) => true,

            (ApplicationStatus.AutoApproved, ApplicationStatus.Approved) => true,
            (ApplicationStatus.AutoApproved, ApplicationStatus.Withdrawn) => true,

            (ApplicationStatus.UnderReview, ApplicationStatus.Approved) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
            (ApplicationStatus.UnderReview, ApplicationStatus.Withdrawn) => true,

            _ => false
        };
    }

    public void ChangeStatus(ApplicationStatus targetStatus)
    {
        if (Status == targetStatus)
        {
            return;
        }

        if (!CanTransitionTo(targetStatus))
        {
            throw new BusinessRuleException(
                $"Переход из статуса '{Status}' в статус '{targetStatus}' невозможен.",
                statusCode: 409);
        }

        Status = targetStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplySnapshot(decimal income, decimal existingPayments, int age)
    {
        IncomeAtApply = income;
        ExistingPaymentsAtApply = existingPayments;
        AgeAtApply = age;
    }
}
