// KufeArtFullAdission.GarsonMvc/Services/InactiveTableMonitorService.cs
using AppDbContext;
using KufeArtFullAdission.GarsonMvc.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KufeArtFullAdission.GarsonMvc.Services;

public class InactiveTableMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<WaiterHub> _hubContext;
    private readonly ILogger<InactiveTableMonitorService> _logger;
    private readonly int _checkIntervalMinutes = 1; // Her 5 dakikada bir kontrol et
    private readonly int _inactiveThresholdMinutes = 2; // 15 dakika eşik

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

    private async Task CheckInactiveTables()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

        var thresholdTime = DateTime.Now.AddMinutes(-_inactiveThresholdMinutes);

        // Sadece aktif masaları kontrol et
        var inactiveTables = await dbContext.Tables
            .Where(t => t.IsActive && t.AddionStatus.HasValue) // Dolu masalar
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

        foreach (var table in inactiveTables)
        {
            var inactiveMinutes = (int)(DateTime.Now - table.LastOrderTime).TotalMinutes;
            await SendWaiterAlert(table.Id, table.Name, inactiveMinutes);
        }

        if (inactiveTables.Any())
        {
            _logger.LogInformation($"🔔 {inactiveTables.Count} masa için garson uyarısı gönderildi");
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

        // Sadece garsonlara bildirim gönder
        await _hubContext.Clients.Group("Waiters").SendAsync("InactiveTableAlert", alertData);
    }
}