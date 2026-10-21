namespace CreditCalculator.Application.Abstractions;

public interface IApplicationStatusNotifier
{
    Task NotifyStatusChangedAsync(Guid applicationId, string newStatus, string? comment = null, CancellationToken cancellationToken = default);
}