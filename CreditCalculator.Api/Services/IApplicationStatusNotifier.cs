using CreditCalculator.Api.Hubs;
using CreditCalculator.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace CreditCalculator.Api.Services;

public interface IApplicationStatusNotifier
{
    Task NotifyStatusChangedAsync(Guid applicationId, string newStatus, string? comment = null, CancellationToken cancellationToken = default);
}

public sealed class ApplicationStatusNotifier : IApplicationStatusNotifier, CreditCalculator.Application.Abstractions.IApplicationStatusNotifier
{
    private readonly IHubContext<ApplicationStatusHub> _hubContext;
    private readonly ILogger<ApplicationStatusNotifier> _logger;

    public ApplicationStatusNotifier(IHubContext<ApplicationStatusHub> hubContext, ILogger<ApplicationStatusNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyStatusChangedAsync(Guid applicationId, string newStatus, string? comment = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"application:{applicationId}").SendAsync("StatusChanged", new
            {
                ApplicationId = applicationId,
                NewStatus = newStatus,
                Comment = comment,
                ChangedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            _logger.LogInformation("SignalR уведомление отправлено для заявки {ApplicationId}: статус {NewStatus}", applicationId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки SignalR уведомления для заявки {ApplicationId}", applicationId);
        }
    }
}