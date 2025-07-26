// KufeArtFullAdission.GarsonMvc/Services/InactiveTableMonitorService.cs
using AppDbContext;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KufeArtFullAdission.GarsonMvc.Services;

public class InactiveTableMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<WaiterHub> _hubContext;
    private readonly ILogger<InactiveTableMonitorService> _logger;
    private readonly int _checkIntervalMinutes = 6; // Her 5 dakikada bir kontrol et
    private readonly int _inactiveThresholdMinutes = 35; // 15 dakika eşik

    public InactiveTableMonitorService(
        IServiceProvider serviceProvider,
        IHubContext<WaiterHub> hubContext,
        ILogger<InactiveTableMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔍 Masa takip servisi başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckInactiveTables();
                await Task.Delay(TimeSpan.FromMinutes(_checkIntervalMinutes), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Masa takip servisi hatası");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }

    // InactiveTableMonitorService.cs - CheckInactiveTables metodunda
    private async Task CheckInactiveTables()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

        var thresholdTime = DateTime.Now.AddMinutes(-_inactiveThresholdMinutes);

        _logger.LogInformation($"🔍 Masa kontrolü başlıyor. Şu an: {DateTime.Now}, Eşik: {thresholdTime}");

        // Önce tüm aktif masaları listele
        var allActiveTables = await dbContext.Tables
            .Where(t => t.IsActive && t.AddionStatus.HasValue)
            .Select(t => new { t.Name, t.AddionStatus })
            .ToListAsync();

        _logger.LogInformation($"📋 Aktif masa sayısı: {allActiveTables.Count}");

        var inactiveTables = await dbContext.Tables
            .Where(t => t.IsActive && t.AddionStatus.HasValue)
            .Select(t => new
            {
                t.Id,
                t.Name,
                LastOrderTime = dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == t.AddionStatus)
                    .Max(h => h.CreatedAt)
            })
            .Where(t => t.LastOrderTime < thresholdTime)
            .ToListAsync();

        _logger.LogInformation($"⏰ İnaktif masa sayısı: {inactiveTables.Count}");

        foreach (var table in inactiveTables)
        {
            var inactiveMinutes = (int)(DateTime.Now - table.LastOrderTime).TotalMinutes;
            _logger.LogInformation($"🚨 {table.Name} masası {inactiveMinutes} dakikadır inaktif - Bildirim gönderiliyor");
            await SendWaiterAlert(table.Id, table.Name, inactiveMinutes);
        }
    }

    private async Task SendWaiterAlert(Guid tableId, string tableName, int inactiveMinutes)
    {
        var alertData = new
        {
            Type = "InactiveTable",
            TableId = tableId,
            TableName = tableName,
            Message = $"{tableName} - {inactiveMinutes} dakikadır sipariş yok",
            InactiveMinutes = inactiveMinutes,
            Timestamp = DateTime.Now
        };

        _logger.LogInformation($"📤 {tableName} için bildirim gönderiliyor: {alertData.Message}");

        // Sadece garsonlara bildirim gönder
        await _hubContext.Clients.Group("Waiters").SendAsync("InactiveTableAlert", alertData);

        _logger.LogInformation($"✅ {tableName} bildirimi gönderildi");
    }
}