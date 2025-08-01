﻿// KufeArtFullAdission.GarsonMvc/Controllers/OrderController.cs
using AppDbContext;
using Azure;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.GarsonMvc.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

[Authorize]
public class OrderController(DBContext _dbContext) : Controller
{
    public async Task<IActionResult> Index(Guid tableId, string tableName, bool isOccupied = false)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table == null)
            {
                TempData["ErrorMessage"] = "Masa bulunamadı!";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new
            {
                TableId = tableId,
                TableName = tableName,
                IsOccupied = isOccupied,
                WaiterName = User.GetFullName()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Masa bilgileri yüklenemedi!";
            return RedirectToAction("Index", "Home");
        }
    }


    [HttpPost]
    public async Task<IActionResult> CancelOrderItem([FromBody] CancelOrderItemRequest request)
    {
        try
        {
            Console.WriteLine($"❌ Sipariş item iptal başlatılıyor: {request.OrderItemId}");

            // Sipariş item'ı bul
            var orderItem = await _dbContext.AddtionHistories
                .FirstOrDefaultAsync(h => h.Id == request.OrderItemId);

            if (orderItem == null)
            {
                Console.WriteLine($"❌ Sipariş bulunamadı: {request.OrderItemId}");
                return Json(new { success = false, message = "Sipariş bulunamadı!" });
            }

            Console.WriteLine($"✅ Sipariş bulundu: {orderItem.ProductName}");

            // ✅ YENİ: Silmek yerine iptal olarak işaretle
            orderItem.IsCancelled = true;
            orderItem.CancelReason = request.CancelReason ?? "Garson tarafından iptal edildi";
            orderItem.CancelledAt = DateTime.Now;
            orderItem.CancelledBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            orderItem.CancelledByName = User.GetFullName();

            // ❌ ESKİ KOD - Bu satırı KALDIRELIM:
            // _dbContext.AddtionHistories.Remove(orderItem);

            await _dbContext.SaveChangesAsync();

            var table = await _dbContext.Tables.FindAsync(orderItem.TableId);
            var waiterName = User.GetFullName();
            var productName = orderItem.ProductName;
            var tableName = table?.Name ?? "Bilinmeyen Masa";

            // Bildirim gönder
            await SendOrderCancelNotification(tableName, productName, waiterName);

            Console.WriteLine($"✅ Sipariş iptal edildi: {productName}");

            return Json(new
            {
                success = true,
                message = $"{productName} siparişi iptal edildi!",
                cancelledItem = new
                {
                    id = orderItem.Id,
                    productName = productName,
                    isCancelled = true
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 İptal hatası: {ex.Message}");
            return Json(new { success = false, message = "Hata: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTableDetails(Guid tableId)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table == null)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            var isOccupied = table.AddionStatus.HasValue;
            var orders = new List<object>();
            var totalAmount = 0.0;
            var openedAt = (DateTime?)null;

            if (isOccupied)
            {
                // ✅ YENİ: TÜM siparişleri getir (iptal edilenler dahil)
                var orderHistory = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == table.AddionStatus)
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new
                    {
                        h.ProductName,
                        h.ProductQuantity,
                        h.ProductPrice,
                        h.TotalPrice,
                        h.ShorLabel,
                        h.CreatedAt,
                        h.PersonFullName,
                        h.IsCancelled    // ✅ YENİ ALAN
                    })
                    .ToListAsync();

                orders = orderHistory.Cast<object>().ToList();

                // ✅ YENİ: Sadece iptal edilmeyenlerin toplamını hesapla
                totalAmount = orderHistory.Where(o => !o.IsCancelled).Sum(o => o.TotalPrice);

                openedAt = orderHistory.FirstOrDefault()?.CreatedAt;
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    tableId = table.Id,
                    tableName = table.Name,
                    category = table.Category,
                    isOccupied = isOccupied,
                    totalAmount = totalAmount,  // ✅ Artık sadece iptal edilmeyenler
                    openedAt = openedAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    orders = orders
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Masa detayları yüklenemedi: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        try
        {
            var products = await _dbContext.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.CategoryName)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    price = p.Price,
                    categoryName = p.CategoryName,
                    type = p.Type.ToString(),
                    hasCampaign = p.HasCampaign,
                    campaignCaption = p.CampaignCaption,
                    // Ürün resmini de ekleyebiliriz
                    imageUrl = "/images/products/default.jpg" // Varsayılan resim
                })
                .ToListAsync();

            // Kategorilere göre grupla
            var categories = products.GroupBy(p => p.categoryName)
                                   .Select(g => new
                                   {
                                       name = g.Key,
                                       products = g.ToList(),
                                       count = g.Count()
                                   })
                                   .OrderBy(c => c.name)
                                   .ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    categories = categories,
                    allProducts = products
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Ürünler yüklenemedi: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] OrderSubmissionDto orderDto)
    {
        try
        {
            if (orderDto?.Items == null || !orderDto.Items.Any())
                return Json(new { success = false, message = "Sepet boş!" });

            var table = await _dbContext.Tables.FindAsync(orderDto.TableId);
            if (table == null)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            // AddionStatus kontrolü
            Guid addionStatusId;
            if (table.AddionStatus == null)
            {
                // İlk sipariş - Yeni AddionStatus oluştur
                addionStatusId = Guid.NewGuid();
                table.AddionStatus = addionStatusId;
            }
            else
            {
                // Mevcut sipariş - Var olan AddionStatus'u kullan
                addionStatusId = table.AddionStatus.Value;
            }

            // Sipariş batch ID'si oluştur
            var batchId = Guid.NewGuid();
            var currentUserId = User.GetUserId();
            var currentUser = User.GetFullName();

            // Her ürün için sipariş kaydı oluştur
            foreach (var item in orderDto.Items)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                var orderHistory = new AddtionHistoryDbEntity
                {
                    TableId = orderDto.TableId,
                    AddionStatusId = addionStatusId,
                    OrderBatchId = batchId,
                    ShorLabel = orderDto.WaiterNote ?? "",
                    ProductName = product.Name,
                    ProductPrice = product.Price,
                    ProductQuantity = item.Quantity,
                    TotalPrice = product.Price * item.Quantity,
                    PersonId = currentUserId,
                    PersonFullName = currentUser
                };

                _dbContext.AddtionHistories.Add(orderHistory);
            }

            await _dbContext.SaveChangesAsync();

            // 🚀 BURADA SIGNALR İLE ADMİN PANELİNE BİLDİRİM GÖNDERECEĞİZ
            await NotifyAdminPanel(table.Id, table.Name, orderDto.Items.Sum(i => i.Quantity * i.Price));

            var totalAmount = orderDto.Items.Sum(i => i.Quantity * i.Price);
            return Json(new
            {
                success = true,
                message = $"Sipariş başarıyla alındı! Toplam: {totalAmount:C2}",
                data = new
                {
                    batchId = batchId,
                    totalAmount = totalAmount,
                    tableId = table.Id,
                    tableName = table.Name
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Sipariş gönderilemedi: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderHistory(Guid tableId)
    {
        try
        {
            Console.WriteLine($"📋 GetOrderHistory çağrıldı - TableId: {tableId}");

            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table == null)
            {
                Console.WriteLine("❌ Masa bulunamadı");
                return Json(new { success = false, message = "Masa bulunamadı!" });
            }

            if (!table.AddionStatus.HasValue)
            {
                Console.WriteLine("ℹ️ Masa boş - Sipariş geçmişi yok");
                return Json(new { success = true, data = new List<object>() });
            }

            // ✅ YENİ ALANLAR DAHİL EDİLDİ
            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.ProductName,
                    h.ProductQuantity,
                    h.ProductPrice,
                    h.TotalPrice,
                    h.ShorLabel,
                    h.PersonFullName,
                    h.CreatedAt,
                    FormattedTime = h.CreatedAt.ToString("HH:mm"),

                    // ✅ YENİ: İptal ve ödeme durumları
                    IsCancelled = h.IsCancelled,
                    CancelReason = h.CancelReason,
                    CancelledAt = h.CancelledAt,
                    CancelledByName = h.CancelledByName,
                    IsPaid = h.IsPaid,
                    PaidAt = h.PaidAt
                })
                .ToListAsync();

            Console.WriteLine($"✅ {orders.Count} sipariş getirildi");
            return Json(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 GetOrderHistory hatası: {ex.Message}");
            return Json(new { success = false, message = "Sipariş geçmişi yüklenemedi: " + ex.Message });
        }
    }


    [HttpGet]
    public async Task<IActionResult> History(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Varsayılan olarak son 7 gün
            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today.AddDays(1);

            var orders = await _dbContext.AddtionHistories
                .Where(h => h.PersonId == currentUserId &&
                           h.CreatedAt >= start &&
                           h.CreatedAt < end)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.ProductName,
                    h.ProductQuantity,
                    h.ProductPrice,
                    h.TotalPrice,
                    h.ShorLabel,
                    h.CreatedAt,
                    h.TableId,
                    TableName = _dbContext.Tables.Where(t => t.Id == h.TableId).Select(t => t.Name).FirstOrDefault()
                })
                .ToListAsync();

            // Günlük grupla
            var groupedOrders = orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Orders = g.ToList(),
                    TotalAmount = g.Sum(o => o.TotalPrice),
                    OrderCount = g.Count()
                })
                .OrderByDescending(g => g.Date)
                .ToList();

            var viewModel = new
            {
                StartDate = start,
                EndDate = end.AddDays(-1),
                WaiterName = User.Identity.Name,
                GroupedOrders = groupedOrders,
                TotalAmount = orders.Sum(o => o.TotalPrice),
                TotalOrders = orders.Count
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Geçmiş yüklenirken hata oluştu: " + ex.Message;
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetHistoryData(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var start = startDate ?? DateTime.Today.AddDays(-7);
            var end = endDate ?? DateTime.Today.AddDays(1);

            var orders = await _dbContext.AddtionHistories
                .Where(h => h.PersonId == currentUserId &&
                           h.CreatedAt >= start &&
                           h.CreatedAt < end)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.ProductName,
                    h.ProductQuantity,
                    h.ProductPrice,
                    h.TotalPrice,
                    h.ShorLabel,
                    h.CreatedAt,
                    FormattedDate = h.CreatedAt.ToString("dd.MM.yyyy"),
                    FormattedTime = h.CreatedAt.ToString("HH:mm"),
                    TableName = _dbContext.Tables.Where(t => t.Id == h.TableId).Select(t => t.Name).FirstOrDefault()
                })
                .ToListAsync();

            return Json(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    // KufeArtFullAdission.GarsonMvc/Controllers/OrderController.cs - NotifyAdminPanel metodunu güncelle

    private async Task NotifyAdminPanel(Guid tableId, string tableName, double totalAmount)
    {
        try
        {
            // Sipariş detaylarını çek
            var orderItems = await GetOrderItemsForNotification(tableId);

            var notificationData = new
            {
                TableId = tableId,
                TableName = tableName,
                TotalAmount = totalAmount,
                WaiterName = User.GetFullName(),
                Timestamp = DateTime.Now,
                Items = orderItems // Sipariş detayları eklendi
            };

            // 1. Admin paneline bildirim
            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AdminPanel");
            await httpClient.PostAsJsonAsync("/api/notification/new-order", notificationData);

            // 2. ✅ Tablet projesine SADECE İLGİLİ DEPARTMANA bildirim gönder
            var tabletClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("TabletPanel");

            // Mutfak ürünleri var mı kontrol et
            var hasKitchenItems = orderItems.Any(item => item.GetType().GetProperty("ProductType")?.GetValue(item)?.ToString() == "Kitchen");

            // Bar ürünleri var mı kontrol et  
            var hasBarItems = orderItems.Any(item => item.GetType().GetProperty("ProductType")?.GetValue(item)?.ToString() == "Bar");

            // ✅ Sadece ilgili departmanlara bildirim gönder
            if (hasKitchenItems)
            {
                await tabletClient.PostAsJsonAsync("/api/notification/tablet-kitchen", new
                {
                    OrderData = notificationData,
                    Department = "Kitchen"
                });
                Console.WriteLine("🍳 Mutfak tabletine bildirim gönderildi");
            }

            if (hasBarItems)
            {
                await tabletClient.PostAsJsonAsync("/api/notification/tablet-bar", new
                {
                    OrderData = notificationData,
                    Department = "Bar"
                });
                Console.WriteLine("🍹 Bar tabletine bildirim gönderildi");
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Bildirim hatası: {ex.Message}");
        }
    }


    // ✅ YENİ: Sipariş detaylarını çekme metodu
    private async Task<List<object>> GetOrderItemsForNotification(Guid tableId)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table?.AddionStatus == null)
                return new List<object>();

            // En son batch'i al (son sipariş)
            var latestBatch = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => h.OrderBatchId)
                .FirstOrDefaultAsync();

            if (latestBatch == Guid.Empty)
                return new List<object>();

            // Batch'e ait ürünleri al ve ProductDbEntity ile join et
            var orderItems = await (from history in _dbContext.AddtionHistories
                                    join product in _dbContext.Products on history.ProductName equals product.Name
                                    where history.OrderBatchId == latestBatch
                                    select new
                                    {
                                        ProductName = history.ProductName,
                                        Quantity = history.ProductQuantity,
                                        Price = history.ProductPrice,
                                        TotalPrice = history.TotalPrice,
                                        CategoryName = product.CategoryName,
                                        Description=product.Description,
                                        ProductType = product.Type == ProductOrderType.Kitchen ? "Kitchen" : "Bar"
                                    }).ToListAsync();

            return orderItems.Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            // Hata durumunda boş liste döndür
            System.Diagnostics.Debug.WriteLine($"❌ Sipariş detayları alınamadı: {ex.Message}");
            return new List<object>();
        }
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

            var res = await httpClient.PostAsJsonAsync("/api/notification/order-cancelled", notification);
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

public class CancelOrderItemRequest
{
    public Guid OrderItemId { get; set; }
    public string CancelReason { get; set; }
}

// DTO Classes
public class OrderSubmissionDto
{
    public Guid TableId { get; set; }
    public string? WaiterNote { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public double Price { get; set; }
}