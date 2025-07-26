// KufeArtFullAdission.GarsonMvc/Hubs/WaiterHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace KufeArtFullAdission.GarsonMvc.Hubs;

[Authorize]
public class WaiterHub : Hub
{
    public async Task JoinWaiterGroup(string waiterName)
    {
        // 🔥 "Waiters" grubunu kullan (BackgroundService ile uyumlu)
        await Groups.AddToGroupAsync(Context.ConnectionId, "Waiters");
        await Clients.Caller.SendAsync("JoinedWaiterGroup", $"Garson: {waiterName}");
    }

    public async Task RefreshTableData()
    {
        await Clients.Group("Waiters").SendAsync("RefreshTables");
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Waiters");
        await base.OnDisconnectedAsync(exception);
    }
}