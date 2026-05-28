using Microsoft.AspNetCore.SignalR;

namespace SmartCityPulse.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinDepartment(string department)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, department);
        }
    }
}