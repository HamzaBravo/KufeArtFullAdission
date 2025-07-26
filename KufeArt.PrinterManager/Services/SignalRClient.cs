using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KufeArt.PrinterManager.Services;

// KufeArt.PrinterManager/Services/SignalRClient.cs
public class SignalRClient
{
    private HubConnection? _connection;

    public async Task ConnectAsync()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl("https://adisyon.kufeart.com/orderHub") // Adisyon panel hub'ı
            .Build();

        _connection.On<OrderNotificationModel>("NewOrderReceived", OnNewOrderReceived);
        await _connection.StartAsync();
    }

    private async Task OnNewOrderReceived(OrderNotificationModel order)
    {
        // Sipariş geldi, yazdırma işlemini başlat
        await PrintOrderAsync(order);
    }
}
