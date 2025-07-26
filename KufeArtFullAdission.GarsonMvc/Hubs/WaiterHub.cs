// KufeArtFullAdission.GarsonMvc/Hubs/WaiterHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

[Authorize]
public class WaiterHub : Hub
{
    public async Task JoinWaiterGroup(string waiterName)
    {
        // Her iki gruba da katıl
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllWaiters"); // Mevcut sistem
        await Groups.AddToGroupAsync(Context.ConnectionId, "Waiters");    // Yeni inaktivite sistemi

        await Clients.Caller.SendAsync("JoinedWaiterGroup", $"Garson: {waiterName}");
    }

    public async Task RefreshTableData()
    {
        await Clients.Group("AllWaiters").SendAsync("RefreshTables");
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AllWaiters");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Waiters");
        await base.OnDisconnectedAsync(exception);
    }
}