using KufeArt.PrinterService.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace KufeArt.PrinterService.Services
{
    public class SignalRClientService
    {
        private readonly PrinterManagerService _printerManager;
        private readonly ConfigurationService _configService;
        private HubConnection _hubConnection;
        private bool _isConnected = false;
        private readonly string _hubUrl;

        public SignalRClientService(
            PrinterManagerService printerManager,
            ConfigurationService configService,
            IConfiguration configuration)
        {
            _printerManager = printerManager;
            _configService = configService;

            // appsettings.json'dan hub URL'ini al
            _hubUrl = configuration.GetConnectionString("https://adisyon.kufeart.com/printerHub");
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                }

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromMinutes(1)
                    })
                    .Build();

                // Event handlers
                _hubConnection.On<object>("PrintOrder", HandlePrintOrderAsync);
                _hubConnection.On("RequestStatus", HandleStatusRequestAsync);
                _hubConnection.On("ReloadConfig", HandleReloadConfigAsync);

                // Connection events
                _hubConnection.Closed += OnConnectionClosed;
                _hubConnection.Reconnected += OnReconnected;
                _hubConnection.Reconnecting += OnReconnecting;

                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinPrinterServiceGroup");

                _isConnected = true;

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                return false;
            }
        }

        private async Task HandlePrintOrderAsync(object orderDataObj)
        {
            try
            {

                var json = JsonConvert.SerializeObject(orderDataObj);
                var orderData = JsonConvert.DeserializeObject<OrderPrintData>(json);

                if (orderData == null)
                {
                    return;
                }

                // Konfigürasyon kontrolü
                if (!_configService.HasValidConfiguration())
                {
                    return;
                }

                bool success = await _printerManager.PrintOrderAsync(orderData);

                if (success)
                {
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async Task HandleStatusRequestAsync()
        {
            try
            {
                var status = _printerManager.GetStatus();

                if (_hubConnection?.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SendServiceStatus", status);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void HandleReloadConfigAsync()
        {
            try
            {
                _configService.ReloadConfigurationAsync();
            }
            catch (Exception ex)
            {
            }
        }

        private Task OnConnectionClosed(Exception exception)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        private Task OnReconnected(string connectionId)
        {
            _isConnected = true;
            return Task.CompletedTask;
        }

        private Task OnReconnecting(Exception exception)
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        public bool IsConnected => _isConnected;

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _isConnected = false;
            }
        }
    }
}

