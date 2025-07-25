// KufeArtFullAdission.GarsonMvc/Hubs/WaiterHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace KufeArtFullAdission.GarsonMvc.Hubs;

[Authorize]
public class WaiterHub : Hub
{
    public async Task JoinWaiterGroup(string waiterName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AllWaiters");
        await Clients.Caller.SendAsync("JoinedWaiterGroup", $"Garson: {waiterName}");
    }

    public async Task RefreshTableData()
    {
        // Tüm garsonlara masa listesini yenile komutu gönder
        await Clients.Group("AllWaiters").SendAsync("RefreshTables");
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}