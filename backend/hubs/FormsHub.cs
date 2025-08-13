using Microsoft.AspNetCore.SignalR;

namespace SupportBot.Hubs;

public class FormsHub : Hub
{
    private static readonly Dictionary<string, string> _userConnections = new();

    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();
        if (!string.IsNullOrEmpty(sessionId))
        {
            _userConnections[sessionId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_userConnections)
        {
            var item = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(item.Key))
            {
                _userConnections.Remove(item.Key);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

}