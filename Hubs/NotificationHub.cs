// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace SmartCityPulse.Hubs
{
    public class NotificationHub : Hub
    {
        // Clients call this to join role-based groups
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // Server-side methods (called from controllers via IHubContext)
        // are not needed here – we'll use IHubContext directly.
    }
}