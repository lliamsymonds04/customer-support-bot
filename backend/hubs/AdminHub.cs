using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SupportBot.Models;

namespace SupportBot.Hubs;

[Authorize(Roles = "Admin")]
public class AdminHub : Hub
{
    // Hub methods for admin functionalities
    public async Task SendFormToAdmins(Form form)
    {
        // Logic to send form to all admins
        await Clients.Group("Admins").SendAsync("AdminReceiveForm", form);
    }
}