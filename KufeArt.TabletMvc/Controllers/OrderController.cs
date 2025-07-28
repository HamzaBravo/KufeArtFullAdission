// KufeArt.TabletMvc/Controllers/OrderController.cs
using AppDbContext;
using KufeArt.TabletMvc.Models;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KufeArt.TabletMvc.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly DBContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrderController(DBContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("api/orders")]
    public async Task<IActionResult> GetOrders(string? status = null)
    {
        try
        {
            // Sadece ilk yükleme için - polling için değil
            Console.WriteLine("📋 İlk sipariş yüklemesi");

            var department = User.FindFirst("Department")?.Value;
            if (string.IsNullOrEmpty(department))
            {
                return Json(new { success = false, message = "Departman bilgisi bulunamadı" });
            }

            var productType = department == "Kitchen" ? ProductOrderType.Kitchen : ProductOrderType.Bar;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Siparişleri getir (tek seferlik)
            var query = from history in _context.AddtionHistories
                        join product in _context.Products on history.ProductName equals product.Name
                        join table in _context.Tables on history.TableId equals table.Id
                        join batchStatus in _context.OrderBatchStatuses on history.OrderBatchId equals batchStatus.OrderBatchId into statusGroup
                        from batchStatus in statusGroup.DefaultIfEmpty()
                        where history.CreatedAt >= today
                              && history.CreatedAt < tomorrow
                              && product.Type == productType
                        select new
                        {
                            history.OrderBatchId,
                            history.TableId,
                            TableName = table.Name,
                            history.PersonFullName,
                            history.CreatedAt,
                            history.ProductName,
                            history.ProductQuantity,
                            history.ProductPrice,
                            history.ShorLabel,
                            ProductType = product.Type.ToString(),
                            IsReady = batchStatus != null ? batchStatus.IsReady : false,
                            CompletedAt = batchStatus != null ? batchStatus.CompletedAt : null
                        };

            var orders = await query.ToListAsync();

            var groupedOrders = orders
                .GroupBy(o => o.OrderBatchId)
                .Select(g => new
                {
                    orderBatchId = g.Key,
                    tableId = g.First().TableId,
                    tableName = g.First().TableName,
                    waiterName = g.First().PersonFullName,
                    orderTime = g.Min(o => o.CreatedAt),
                    status = g.First().IsReady ? "Ready" : "New",
                    totalAmount = g.Sum(o => o.ProductPrice * o.ProductQuantity),
                    items = g.Select(o => new
                    {
                        productName = o.ProductName,
                        quantity = o.ProductQuantity,
                        price = o.ProductPrice,
                        categoryName = o.ProductType
                    }).ToList(),
                    note = g.First().ShorLabel,
                    isNew = false // İlk yükleme için yeni değil
                })
                .OrderBy(o => o.orderTime)
                .ToList();

            return Json(new
            {
                success = true,
                data = new { orders = groupedOrders }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Status Update Method
    [HttpPost("api/orders/{orderBatchId}/ready")]
    public async Task<IActionResult> MarkAsReady(string orderBatchId)
    {
        try
        {
            if (!Guid.TryParse(orderBatchId, out var batchId))
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID" });
            }

            var department = User.FindFirst("Department")?.Value;
            var tabletName = User.Identity?.Name;

            // Mevcut status var mı kontrol et
            var existingStatus = await _context.OrderBatchStatuses
                .FirstOrDefaultAsync(s => s.OrderBatchId == batchId);

            if (existingStatus == null)
            {
                // Yeni status oluştur
                var newStatus = new OrderBatchStatusDbEntity
                {
                    OrderBatchId = batchId,
                    IsReady = true,
                    CompletedBy = tabletName,
                    Department = department,
                    CompletedAt = DateTime.Now
                };
                _context.OrderBatchStatuses.Add(newStatus);
            }
            else
            {
                // Mevcut status'u güncelle
                existingStatus.IsReady = true;
                existingStatus.CompletedBy = tabletName;
                existingStatus.Department = department;
                existingStatus.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Sipariş hazır olarak işaretlendi",
                data = new { orderBatchId, status = "Ready" }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Durum güncellenemedi: {ex.Message}" });
        }
    }

    // Helper Methods
    private string GetSimpleStatus(bool isReady, DateTime orderTime)
    {
        if (isReady) return "Ready";

        var elapsed = DateTime.Now - orderTime;
        return elapsed.TotalMinutes < 5 ? "New" : "Preparing";
    }

    private bool IsNewOrder(DateTime orderTime)
    {
        return (DateTime.Now - orderTime).TotalMinutes < 3;
    }


    [HttpGet("api/orders/{orderBatchId}")]
    public async Task<IActionResult> GetOrderDetail(string orderBatchId)
    {
        try
        {
            if (!Guid.TryParse(orderBatchId, out var batchId))
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID" });
            }

            var department = User.FindFirst("Department")?.Value;
            var productType = department == "Kitchen" ? ProductOrderType.Kitchen : ProductOrderType.Bar;

            var orderItems = await (from history in _context.AddtionHistories
                                    join product in _context.Products on history.ProductName equals product.Name
                                    join table in _context.Tables on history.TableId equals table.Id
                                    where history.OrderBatchId == batchId && product.Type == productType
                                    select new
                                    {
                                        history.OrderBatchId,
                                        history.TableId,
                                        TableName = table.Name,
                                        history.PersonFullName,
                                        history.CreatedAt,
                                        history.ProductName,
                                        history.ProductQuantity,
                                        history.ProductPrice,
                                        history.ShorLabel,
                                        ProductDescription = product.Description
                                    }).ToListAsync();

            if (!orderItems.Any())
            {
                return Json(new { success = false, message = "Sipariş bulunamadı" });
            }

            var firstItem = orderItems.First();
            var orderDetail = new
            {
                orderBatchId = firstItem.OrderBatchId.ToString(),
                tableId = firstItem.TableId.ToString(),
                tableName = firstItem.TableName,
                waiterName = firstItem.PersonFullName,
                orderTime = firstItem.CreatedAt,
                status = GetSimpleStatus(false, firstItem.CreatedAt), // Geçici olarak false
                note = firstItem.ShorLabel,
                totalAmount = orderItems.Sum(x => x.ProductPrice * x.ProductQuantity),
                items = orderItems.Select(x => new
                {
                    productName = x.ProductName,
                    quantity = x.ProductQuantity,
                    unitPrice = x.ProductPrice,
                    totalPrice = x.ProductPrice * x.ProductQuantity,
                    description = x.ProductDescription
                }).ToList()
            };

            return Json(new { success = true, data = orderDetail });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Sipariş detayı yüklenemedi: {ex.Message}" });
        }
    }

    [HttpPost("api/orders/{orderBatchId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(string orderBatchId, [FromBody] UpdateOrderStatusModel model)
    {
        try
        {
            if (!Guid.TryParse(orderBatchId, out var batchId))
            {
                return Json(new { success = false, message = "Geçersiz sipariş ID" });
            }

            // Şimdilik sadece Ready status'u destekliyoruz
            if (model.Status != "Ready")
            {
                return Json(new { success = false, message = "Sadece 'Hazır' durumu güncellenebilir" });
            }

            // Bu sipariş için bir status tablosu yoksa, şimdilik log'a yazdıralım
            // Gelecekte OrderStatus tablosu ekleyebiliriz
            var tabletName = User.Identity?.Name;
            var updateTime = DateTime.Now;

            // Burada SignalR ile admin paneline bildirim gönderilebilir
            // await NotifyOrderStatusChange(orderBatchId, model.Status);

            return Json(new
            {
                success = true,
                message = "Sipariş durumu güncellendi",
                data = new
                {
                    orderBatchId,
                    newStatus = model.Status,
                    updatedBy = tabletName,
                    updatedAt = updateTime
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Durum güncellenemedi: {ex.Message}" });
        }
    }

    private string GetOrderStatus(DateTime orderTime)
    {
        var elapsed = DateTime.Now - orderTime;

        if (elapsed.TotalMinutes < 5)
            return "New";
        else if (elapsed.TotalMinutes < 20)
            return "InProgress";
        else
            return "Ready"; // 20+ dakika sonra otomatik Ready sayılabilir
    }

}