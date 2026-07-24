using CarPlates.Shared.Constants;
using Microsoft.AspNetCore.SignalR;

namespace CarPlates.API.Hubs;

public class ReceivedIP : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

public class IPUpdateMessage
{
    public string Id { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string NewIP { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
