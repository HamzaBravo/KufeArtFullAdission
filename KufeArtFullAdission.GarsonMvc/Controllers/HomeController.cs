// KufeArtFullAdission.GarsonMvc/Controllers/HomeController.cs
using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.GarsonMvc.Extensions;
using KufeArtFullAdission.GarsonMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

[Authorize]
public class HomeController(DBContext _dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        var dashboardData = new
        {
            WaiterName = User.GetFullName(),
            ActiveTableCount = await GetActiveTableCount(),
            TodayOrderCount = await GetTodayOrderCount()
        };

        return View(dashboardData);
    }

    [HttpGet]
    public async Task<IActionResult> GetTables()
    {
        try
        {
            var tables = await _dbContext.Tables
                .Where(t => t.IsActive)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var groupedTables = new Dictionary<string, List<object>>();

            foreach (var table in tables)
            {
                // Masa sipariş kontrolü
                var orders = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == table.AddionStatus)
                    .OrderBy(h => h.CreatedAt)
                    .ToListAsync();

                var hasOrders = orders.Any();
                var totalOrderAmount = orders.Sum(o => o.TotalPrice);

                // Ödeme kontrolü
                var totalPaidAmount = 0.0;
                if (table.AddionStatus.HasValue)
                {
                    totalPaidAmount = await _dbContext.Payments
                        .Where(p => p.AddionStatusId == table.AddionStatus)
                        .SumAsync(p => p.Amount);
                }

                var remainingAmount = Math.Max(0, totalOrderAmount - totalPaidAmount);
                var firstOrderTime = hasOrders ? orders.First().CreatedAt : (DateTime?)null;

                // Masa bilgisi
                var tableInfo = new
                {
                    id = table.Id,
                    name = table.Name,
                    category = table.Category,
                    isOccupied = hasOrders,
                    totalAmount = totalOrderAmount,
                    remainingAmount = remainingAmount,
                    openedAt = firstOrderTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    duration = firstOrderTime.HasValue ?
                        DateTime.Now.Subtract(firstOrderTime.Value).TotalMinutes : 0
                };

                if (!groupedTables.ContainsKey(table.Category))
                    groupedTables[table.Category] = new List<object>();

                groupedTables[table.Category].Add(tableInfo);
            }

            return Json(new { success = true, data = groupedTables });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Masalar yüklenemedi: " + ex.Message });
        }
    }


    [HttpPost]
    public async Task<IActionResult> MoveTable([FromBody] MoveTableRequest request)
    {
        try
        {
            var sourceTable = await _dbContext.Tables.FindAsync(request.SourceTableId);
            var targetTable = await _dbContext.Tables.FindAsync(request.TargetTableId);

            if (sourceTable == null || targetTable == null)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            if (!sourceTable.AddionStatus.HasValue)
                return Json(new { success = false, message = "Kaynak masada aktif sipariş yok!" });

            if (targetTable.AddionStatus.HasValue)
                return Json(new { success = false, message = "Hedef masa zaten dolu!" });

            var waiterName = User.GetFullName();

            // Masa taşıma işlemi
            targetTable.AddionStatus = sourceTable.AddionStatus;
            sourceTable.AddionStatus = null;

            // Sipariş geçmişindeki TableId'yi güncelle
            var orders = await _dbContext.AddtionHistories
                .Where(h => h.TableId == sourceTable.Id)
                .ToListAsync();

            foreach (var order in orders)
            {
                order.TableId = targetTable.Id;
            }

            await _dbContext.SaveChangesAsync();

            // ✅ Real-time bildirim gönder
            await SendTableOperationNotification(
                "MoveTable",
                sourceTable,
                targetTable,
                $"📋 {waiterName} tarafından {sourceTable.Name} masası {targetTable.Name} masasına taşındı"
            );

            return Json(new
            {
                success = true,
                message = $"{sourceTable.Name} masası {targetTable.Name} masasına başarıyla taşındı!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Masa taşıma başarısız: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MergeTables([FromBody] MergeTablesRequest request)
    {
        try
        {
            var sourceTable = await _dbContext.Tables.FindAsync(request.SourceTableId);
            var targetTable = await _dbContext.Tables.FindAsync(request.TargetTableId);

            if (sourceTable == null || targetTable == null)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            if (!sourceTable.AddionStatus.HasValue)
                return Json(new { success = false, message = "Kaynak masada aktif sipariş yok!" });

            if (!targetTable.AddionStatus.HasValue)
                return Json(new { success = false, message = "Hedef masada aktif sipariş yok!" });

            var waiterName = User.GetFullName();

            // Kaynak masadaki siparişleri hedef masaya taşı
            var sourceOrders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == sourceTable.AddionStatus)
                .ToListAsync();

            foreach (var order in sourceOrders)
            {
                order.AddionStatusId = targetTable.AddionStatus.Value;
                order.TableId = targetTable.Id;
            }

            // Kaynak masayı temizle
            sourceTable.AddionStatus = null;
            await _dbContext.SaveChangesAsync();

            // ✅ Real-time bildirim gönder
            await SendTableOperationNotification(
                "MergeTables",
                sourceTable,
                targetTable,
                $"🔗 {waiterName} tarafından {sourceTable.Name} masası {targetTable.Name} masası ile birleştirildi"
            );

            return Json(new
            {
                success = true,
                message = $"{sourceTable.Name} masası {targetTable.Name} masası ile başarıyla birleştirildi!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Masa birleştirme başarısız: " + ex.Message });
        }
    }

    // ✅ YENİ: Sipariş İptal İşlemi
    [HttpPost]
    public async Task<IActionResult> CancelTableOrder([FromBody] CancelOrderRequest request)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(request.TableId);
            if (table == null || !table.AddionStatus.HasValue)
                return Json(new { success = false, message = "Masa bulunamadı veya zaten boş!" });

            var tableName = table.Name;
            var waiterName = User.GetFullName();

            // Siparişleri sil
            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .ToListAsync();

            _dbContext.AddtionHistories.RemoveRange(orders);

            // Masa durumunu sıfırla
            table.AddionStatus = null;
            await _dbContext.SaveChangesAsync();

            // ✅ YENİ: Real-time bildirim gönder
            await SendTableOperationNotification(
                "CancelOrder",
                table,
                null, // target table yok
                $"🗑️ {waiterName} tarafından {tableName} masasının siparişi iptal edildi"
            );

            return Json(new
            {
                success = true,
                message = $"{tableName} masasının siparişi başarıyla iptal edildi!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "İşlem başarısız: " + ex.Message });
        }
    }

    private async Task SendTableOperationNotification(string action, TableDbEntity sourceTable, TableDbEntity? targetTable, string customMessage = null)
    {
        try
        {
            var waiterName = User.GetFullName();

            string message = customMessage ?? action switch
            {
                "MoveTable" => $"📋 {waiterName} tarafından {sourceTable.Name} masası {targetTable?.Name} masasına taşındı",
                "MergeTables" => $"🔗 {waiterName} tarafından {sourceTable.Name} masası {targetTable?.Name} ile birleştirildi",
                "CancelOrder" => $"🗑️ {waiterName} tarafından {sourceTable.Name} masasının siparişi iptal edildi",
                _ => $"✅ {waiterName} tarafından masa işlemi gerçekleştirildi"
            };

            var notification = new TableOperationNotificationDto
            {
                
                //Type = "TableOperation",
                Action = action,
                SourceTableId = sourceTable.Id,
                SourceTableName = sourceTable.Name,
                TargetTableId = targetTable?.Id,
                TargetTableName = targetTable?.Name,
                WaiterName = waiterName,
                Message = message,
                Timestamp = DateTime.Now,
                Icon = action switch
                {
                    "MoveTable" => "fas fa-arrows-alt",
                    "MergeTables" => "fas fa-link",
                    "CancelOrder" => "fas fa-times-circle",
                    _ => "fas fa-table"
                },
                Color = action switch
                {
                    "MoveTable" => "#3b82f6",
                    "MergeTables" => "#10b981",
                    "CancelOrder" => "#dc3545",
                    _ => "#6b7280"
                }
            };

            // ✅ 1. HTTP ile admin panele bildirim gönder
            try
            {
                var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AdminPanel");
                var res = await httpClient.PostAsJsonAsync("/api/notification/table-operation", notification);
                Console.WriteLine($"✅ HTTP ile admin panele masa işlemi bildirimi gönderildi: {message}");
            }
            catch (Exception httpEx)
            {
                Console.WriteLine($"⚠️ HTTP bildirim hatası: {httpEx.Message}");
            }

            // ✅ 2. SignalR ile Garson paneline geri bildirim
            try
            {
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<WaiterHub>>();
                await hubContext.Clients.All.SendAsync("TableOperationCompleted", new
                {
                    Action = action,
                    SourceTableName = sourceTable.Name,
                    TargetTableName = targetTable?.Name,
                    WaiterName = waiterName,
                    Message = message,
                    Success = true,
                    Timestamp = DateTime.Now
                });
                Console.WriteLine($"✅ SignalR ile garson paneline masa işlemi bildirimi gönderildi");
            }
            catch (Exception signalREx)
            {
                Console.WriteLine($"⚠️ SignalR bildirim hatası: {signalREx.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Admin panel bildirimi hatası: {ex.Message}");
        }
    }

    private async Task<int> GetActiveTableCount()
    {
        return await _dbContext.Tables
            .Where(t => t.IsActive && t.AddionStatus.HasValue)
            .CountAsync();
    }

    private async Task<int> GetTodayOrderCount()
    {
        var today = DateTime.Today;
        return await _dbContext.AddtionHistories
            .Where(h => h.CreatedAt >= today)
            .Select(h => h.OrderBatchId)
            .Distinct()
            .CountAsync();
    }

    // ✅ YENİ: Sipariş iptal bildirimi helper method
    private async Task SendOrderCancelNotification(string tableName, string productName, string waiterName)
    {
        try
        {
            // 1. HTTP ile admin panele bildirim
            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AdminPanel");
            var notification = new
            {
                Type = "OrderCancelled",
                TableName = tableName,
                ProductName = productName,
                WaiterName = waiterName,
                Message = $"❌ {waiterName} tarafından {tableName} masasından {productName} siparişi iptal edildi",
                Timestamp = DateTime.Now,
                Icon = "fas fa-times-circle",
                Color = "#dc3545"
            };

            await httpClient.PostAsJsonAsync("/api/notification/order-cancelled", notification);
            Console.WriteLine($"✅ Admin panele sipariş iptal bildirimi gönderildi: {productName}");

            // 2. SignalR ile garson paneline
            var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<WaiterHub>>();
            await hubContext.Clients.All.SendAsync("OrderItemCancelled", new
            {
                TableName = tableName,
                ProductName = productName,
                WaiterName = waiterName,
                Message = $"❌ {productName} siparişi iptal edildi",
                Success = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sipariş iptal bildirimi hatası: {ex.Message}");
        }
    }
}

public class TableOperationNotificationDto
{
    public string Action { get; set; } = "";
    public Guid SourceTableId { get; set; }
    public string SourceTableName { get; set; } = "";
    public Guid? TargetTableId { get; set; }
    public string? TargetTableName { get; set; }
    public string WaiterName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}