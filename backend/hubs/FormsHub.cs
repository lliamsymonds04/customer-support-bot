using Microsoft.AspNetCore.SignalR;
using SupportBot.Models;

namespace SupportBot.Hubs;

public class FormsHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new();

    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();
        if (!string.IsNullOrEmpty(sessionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            // _userConnections[sessionId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            Console.WriteLine($"[FormsHub] Removed {Context.ConnectionId} from group {sessionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}