using Microsoft.AspNetCore.SignalR.Client;
using KufeArt.PrinterManager.Models;
using Newtonsoft.Json;

namespace KufeArt.PrinterManager.Services;

public class SignalRClientService
{
    private HubConnection? _connection;
    private bool _isConnected = false;
    private readonly string _hubUrl;

    // Events
    public event Action<bool>? ConnectionStatusChanged;
    public event Action<string>? LogMessageReceived;
    public event Action<OrderNotificationModel>? OrderReceived;

    public SignalRClientService()
    {
      
        _hubUrl = "https://xxxxxxx.com"; // signalR hub URL'sini buraya girin
    }

    public async Task ConnectAsync()
    {
        try
        {
            LogMessage("SignalR bağlantısı kuruluyor...");

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            //  Sipariş bildirimi dinle
            _connection.On<OrderNotificationModel>("NewOrderReceived", OnOrderReceived);

            //  Bağlantı durumu events
            _connection.Closed += OnConnectionClosed;
            _connection.Reconnected += OnReconnected;
            _connection.Reconnecting += OnReconnecting;

            await _connection.StartAsync();
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(true);
            LogMessage("✅ SignalR bağlantısı kuruldu!");
            await JoinPrinterManagerGroup();
        }
        catch (Exception ex)
        {
            LogMessage($"❌ SignalR bağlantı hatası: {ex.Message}");
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
        }
    }

    private void OnOrderReceived(OrderNotificationModel order)
    {
        LogMessage($"📦 Yeni sipariş geldi: {order.TableName} - {order.WaiterName}");
        OrderReceived?.Invoke(order);
    }

    private async Task JoinPrinterManagerGroup()
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.InvokeAsync("JoinPrinterGroup");
                LogMessage("PrinterManager grubuna katıldı");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Grup katılım hatası: {ex.Message}");
        }
    }

    #region Connection Events
    private Task OnConnectionClosed(Exception? error)
    {
        _isConnected = false;
        ConnectionStatusChanged?.Invoke(false);
        LogMessage($"SignalR bağlantısı koptu: {error?.Message ?? "Bilinmeyen sebep"}");
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _isConnected = true;
        ConnectionStatusChanged?.Invoke(true);
        LogMessage("SignalR yeniden bağlandı!");

        _ = Task.Run(JoinPrinterManagerGroup);
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? error)
    {
        _isConnected = false;
        ConnectionStatusChanged?.Invoke(false);
        LogMessage("SignalR yeniden bağlanıyor...");
        return Task.CompletedTask;
    }
    #endregion

    public async Task DisconnectAsync()
    {
        try
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
                LogMessage("SignalR bağlantısı kapatıldı");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Bağlantı kapatma hatası: {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessageReceived?.Invoke(logEntry);

        Console.WriteLine(logEntry);
    }
    public bool IsConnected => _isConnected;
    public string HubUrl => _hubUrl;
}