using KufeArt.PrinterService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KufeArt.PrinterService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SignalRClientService _signalRClient;
        private readonly ConfigurationService _configService;
        private readonly PrinterManagerService _printerManager;

        public Worker(
            ILogger<Worker> logger,
            SignalRClientService signalRClient,
            ConfigurationService configService,
            PrinterManagerService printerManager)
        {
            _logger = logger;
            _signalRClient = signalRClient;
            _configService = configService;
            _printerManager = printerManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🖨️ KufeArt Printer Service başlatıldı - {Time}", DateTimeOffset.Now);

            // Konfigürasyonu yükle
             _configService.LoadConfigurationAsync();

            // SignalR bağlantısını kur
            bool connected = await _signalRClient.ConnectAsync();

            if (!connected)
            {
                _logger.LogWarning("⚠️ SignalR bağlantısı kurulamadı, tekrar denenecek");
            }

            // Ana servis döngüsü
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // SignalR bağlantı kontrolü
                    if (!_signalRClient.IsConnected)
                    {
                        _logger.LogInformation("🔄 SignalR yeniden bağlanmaya çalışılıyor...");
                        await _signalRClient.ConnectAsync();
                    }

                    // Konfigürasyon kontrolü (her 30 saniyede bir)
                    if (DateTime.Now.Second % 30 == 0)
                    {
                         _configService.ReloadConfigurationAsync();
                    }

                    // Heartbeat
                    _logger.LogDebug("💓 Service heartbeat - {Time}", DateTime.Now);

                    await Task.Delay(5000, stoppingToken); // 5 saniye bekle
                }
                catch (OperationCanceledException)
                {
                    // Service durduruluyor
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Service ana döngüsünde hata");
                    await Task.Delay(10000, stoppingToken); // 10 saniye bekle
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 KufeArt Printer Service durduruluyor...");

            await _signalRClient.DisconnectAsync();

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("✅ KufeArt Printer Service durduruldu");
        }
    }
}

