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


    // ✅ YENİ: Masa Taşıma İşlemi
    [HttpPost]
    public async Task<IActionResult> MoveTable([FromBody] MoveTableRequest request)
    {
        try
        {
            Console.WriteLine($"🔄 Masa taşıma başlatılıyor: {request.SourceTableId} -> {request.TargetTableId}");

            // 1. Kaynak ve hedef masaları al
            var sourceTable = await _dbContext.Tables.FindAsync(request.SourceTableId);
            var targetTable = await _dbContext.Tables.FindAsync(request.TargetTableId);

            if (sourceTable == null || targetTable == null)
            {
                return Json(new { success = false, message = "Masa bulunamadı!" });
            }

            // 2. Kaynak masa dolu mu kontrol et
            if (!sourceTable.AddionStatus.HasValue)
            {
                return Json(new { success = false, message = "Kaynak masa zaten boş!" });
            }

            // 3. Hedef masa boş mu kontrol et
            if (targetTable.AddionStatus.HasValue)
            {
                return Json(new { success = false, message = "Hedef masa zaten dolu!" });
            }

            // 4. Taşıma işlemini gerçekleştir
            var sourceAddionStatusId = sourceTable.AddionStatus.Value;

            // Kaynak masayı boşalt
            sourceTable.AddionStatus = null;

            // Hedef masaya taşı
            targetTable.AddionStatus = sourceAddionStatusId;

            // 5. Adisyon geçmişindeki masa bilgilerini güncelle
            var orderHistories = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == sourceAddionStatusId)
                .ToListAsync();

            foreach (var history in orderHistories)
            {
                history.TableId = targetTable.Id;
                // Not: TableName güncellenmez çünkü hangi masadan geldiğini bilmek önemli
            }

            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"✅ Masa taşıma tamamlandı: {sourceTable.Name} -> {targetTable.Name}");

            // 6. Admin panele bildirim gönder
            await NotifyAdminPanelTableChange("move", sourceTable, targetTable);

            return Json(new
            {
                success = true,
                message = $"{sourceTable.Name} başarıyla {targetTable.Name} masasına taşındı!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Masa taşıma hatası: {ex.Message}");
            return Json(new { success = false, message = "Masa taşıma işlemi başarısız!" });
        }
    }

    // ✅ YENİ: Masa Birleştirme İşlemi
    [HttpPost]
    public async Task<IActionResult> MergeTables([FromBody] MergeTablesRequest request)
    {
        try
        {
            Console.WriteLine($"🔗 Masa birleştirme başlatılıyor: {request.SourceTableId} + {request.TargetTableId}");

            // 1. Kaynak ve hedef masaları al
            var sourceTable = await _dbContext.Tables.FindAsync(request.SourceTableId);
            var targetTable = await _dbContext.Tables.FindAsync(request.TargetTableId);

            if (sourceTable == null || targetTable == null)
            {
                return Json(new { success = false, message = "Masa bulunamadı!" });
            }

            // 2. Her iki masa da dolu mu kontrol et
            if (!sourceTable.AddionStatus.HasValue || !targetTable.AddionStatus.HasValue)
            {
                return Json(new { success = false, message = "Birleştirme için her iki masa da dolu olmalı!" });
            }

            var sourceAddionStatusId = sourceTable.AddionStatus.Value;
            var targetAddionStatusId = targetTable.AddionStatus.Value;

            // 3. Kaynak masadaki tüm siparişleri hedef masaya taşı
            var sourceOrderHistories = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == sourceAddionStatusId)
                .ToListAsync();

            foreach (var history in sourceOrderHistories)
            {
                history.AddionStatusId = targetAddionStatusId;
                history.TableId = targetTable.Id;
                // Orijinal masa adını not olarak ekle
                if (string.IsNullOrEmpty(history.ShorLabel))
                {
                    history.ShorLabel = $"({sourceTable.Name}'den taşındı)";
                }
                else
                {
                    history.ShorLabel += $" ({sourceTable.Name}'den taşındı)";
                }
            }

            // 4. Kaynak masayı boşalt
            sourceTable.AddionStatus = null;

            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"✅ Masa birleştirme tamamlandı: {sourceTable.Name} -> {targetTable.Name}");

            // 6. Admin panele bildirim gönder
            await NotifyAdminPanelTableChange("merge", sourceTable, targetTable);

            return Json(new
            {
                success = true,
                message = $"{sourceTable.Name} başarıyla {targetTable.Name} ile birleştirildi!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Masa birleştirme hatası: {ex.Message}");
            return Json(new { success = false, message = "Masa birleştirme işlemi başarısız!" });
        }
    }

    // ✅ YENİ: Sipariş İptal İşlemi
    [HttpPost]
    public async Task<IActionResult> CancelTableOrder([FromBody] CancelOrderRequest request)
    {
        try
        {
            Console.WriteLine($"❌ Sipariş iptal başlatılıyor: {request.TableId}");

            var table = await _dbContext.Tables.FindAsync(request.TableId);
            if (table == null)
            {
                return Json(new { success = false, message = "Masa bulunamadı!" });
            }

            if (!table.AddionStatus.HasValue)
            {
                return Json(new { success = false, message = "Masa zaten boş!" });
            }

            var addionStatusId = table.AddionStatus.Value;

            // 1. Sipariş geçmişini sil
            var orderHistories = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == addionStatusId)
                .ToListAsync();

            _dbContext.AddtionHistories.RemoveRange(orderHistories);

            //// 2. AddionStatus'u sil
            //var addionStatus = await _dbContext.AddionStatuses.FindAsync(addionStatusId);
            //if (addionStatus != null)
            //{
            //    _dbContext.AddionStatuses.Remove(addionStatus);
            //}

            // 3. Masayı boşalt
            table.AddionStatus = null;

            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"✅ Sipariş iptal tamamlandı: {table.Name}");

            // 4. Admin panele bildirim gönder
            await NotifyAdminPanelTableChange("cancel", table, null);

            return Json(new
            {
                success = true,
                message = $"{table.Name} masasının siparişi iptal edildi!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sipariş iptal hatası: {ex.Message}");
            return Json(new { success = false, message = "Sipariş iptal işlemi başarısız!" });
        }
    }

    // ✅ YENİ: Admin Panele Bildirim Gönderme
    private async Task NotifyAdminPanelTableChange(string action, TableDbEntity sourceTable, TableDbEntity? targetTable)
    {
        try
        {
            string message = action switch
            {
                "move" => $"{sourceTable.Name} masası {targetTable?.Name} masasına taşındı",
                "merge" => $"{sourceTable.Name} masası {targetTable?.Name} ile birleştirildi",
                "cancel" => $"{sourceTable.Name} masasının siparişi iptal edildi",
                _ => "Masa işlemi gerçekleştirildi"
            };

            var notification = new
            {
                Type = "TableOperation",
                Action = action,
                SourceTableId = sourceTable.Id,
                SourceTableName = sourceTable.Name,
                TargetTableId = targetTable?.Id,
                TargetTableName = targetTable?.Name,
                Message = message,
                Timestamp = DateTime.Now
            };

            // ✅ 1. HTTP ile admin panele bildirim gönder
            try
            {
                var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AdminPanel");
                await httpClient.PostAsJsonAsync("/api/notification/table-operation", notification);
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
                    Message = message,
                    Success = true
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
}