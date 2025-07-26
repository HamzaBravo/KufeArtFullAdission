// KufeArtFullAdission.Mvc/Hubs/OrderHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminPanel");
        await Clients.Caller.SendAsync("JoinedAdminGroup", "Admin grubuna katıldınız");
    }

    public async Task JoinWaiterGroup(string waiterName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Waiters");
        await Clients.Caller.SendAsync("JoinedWaiterGroup", $"Garson grubu: {waiterName}");
    }

    // 🚀 YENİ: PrinterManager grubu için
    public async Task JoinPrinterGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "PrinterManagers");
        await Clients.Caller.SendAsync("JoinedPrinterGroup", "PrinterManager grubuna katıldı");
    }

    public async Task NotifyNewOrder(object orderData)
    {
        // Admin paneline yeni sipariş bildirimi
        await Clients.Group("AdminPanel").SendAsync("NewOrderReceived", orderData);

        // 🎯 YENİ: PrinterManager'a da gönder
        await Clients.Group("PrinterManagers").SendAsync("NewOrderReceived", orderData);
    }

    public async Task NotifyTableStatusChange(object tableData)
    {
        // Masa durumu değişikliği bildirimi
        await Clients.All.SendAsync("TableStatusChanged", tableData);
    }

    public async Task NotifyOrderComplete(object orderData)
    {
        // Sipariş tamamlandı bildirimi
        await Clients.Group("Waiters").SendAsync("OrderCompleted", orderData);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}