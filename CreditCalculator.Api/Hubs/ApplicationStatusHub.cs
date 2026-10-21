using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CreditCalculator.Api.Hubs;

[Authorize]
public sealed class ApplicationStatusHub : Hub
{
    public async Task SubscribeToApplication(Guid applicationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"application:{applicationId}");
    }

    public async Task UnsubscribeFromApplication(Guid applicationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"application:{applicationId}");
    }
}