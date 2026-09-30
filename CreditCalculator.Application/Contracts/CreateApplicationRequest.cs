namespace CreditCalculator.Application.Contracts;

// Ставки в запросе нет: она берётся из справочника кредитных продуктов на момент подачи заявки.
public sealed record CreateApplicationRequest(
    Guid CreditProductId,
    decimal Amount,
    int TermMonths);
