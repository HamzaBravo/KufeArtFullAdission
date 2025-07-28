// KufeArtFullAdission.GarsonMvc/Controllers/OrderController.cs
using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.GarsonMvc.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                // Mevcut siparişleri getir
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
                        h.PersonFullName
                    })
                    .ToListAsync();

                orders = orderHistory.Cast<object>().ToList();
                totalAmount = orderHistory.Sum(o => o.TotalPrice);
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
                    totalAmount = totalAmount,
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
            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table == null || !table.AddionStatus.HasValue)
                return Json(new { success = true, data = new List<object>() });

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
                    FormattedTime = h.CreatedAt.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, data = orders });
        }
        catch (Exception ex)
        {
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
    private async Task NotifyAdminPanel(Guid tableId, string tableName, double totalAmount)
    {
        try
        {
            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("AdminPanel");

            var notificationData = new
            {
                TableId = tableId,
                TableName = tableName,
                TotalAmount = totalAmount,
                WaiterName = User.GetFullName(),
                Timestamp = DateTime.Now
            };

            var response = await httpClient.PostAsJsonAsync("/api/notification/new-order", notificationData);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Admin paneline bildirim gönderildi: {tableName}");
            }

            // ✅ 2. YENİ: Kitchen/Bar gruplarına direkt SignalR bildirimi gönder
            await NotifyKitchenAndBar(tableId, tableName, totalAmount);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Bildirim hatası: {ex.Message}");
        }
    }


    // ✅ YENİ METHOD: Kitchen/Bar'a direkt bildirim
    private async Task NotifyKitchenAndBar(Guid tableId, string tableName, double totalAmount)
    {
        try
        {
            var orderItems = await GetOrderItemsForNotification(tableId);
            if (!orderItems.Any()) return;

            var kitchenItems = orderItems.Where(x => x.ProductType == "Kitchen").ToList();
            var barItems = orderItems.Where(x => x.ProductType == "Bar").ToList();

            var orderData = new
            {
                Type = "NewOrder",
                TableId = tableId,
                TableName = tableName,
                TotalAmount = totalAmount,
                WaiterName = User.GetFullName(),
                Timestamp = DateTime.Now,
                Items = orderItems.Select(x => new
                {
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    ProductType = x.ProductType,
                    CategoryName = x.CategoryName
                }).ToList()
            };


            // 🚀 YENİ: TabletMvc'ye DOĞRUDAN gönder (daha hızlı)
            var tabletClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("TabletPanel");

            // Kitchen'a bildirim (PARALEL)
            if (kitchenItems.Any())
            {
                var kitchenRequest = new { OrderData = orderData, Department = "Kitchen" };


                // TabletMvc'ye DOĞRUDAN gönder (HIZLI)
                _ = tabletClient.PostAsJsonAsync("/api/notification/tablet-kitchen", kitchenRequest);
            }

            // Bar'a bildirim (PARALEL)
            if (barItems.Any())
            {
                var barRequest = new { OrderData = orderData, Department = "Bar" };

                // TabletMvc'ye DOĞRUDAN gönder (HIZLI)
                _ = tabletClient.PostAsJsonAsync("/api/notification/tablet-bar", barRequest);
            }

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Kitchen/Bar bildirim hatası: {ex.Message}");
        }
    }

    // ✅ HELPER METHOD: Sipariş detaylarını al
    private async Task<List<dynamic>> GetOrderItemsForNotification(Guid tableId)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(tableId);
            if (table?.AddionStatus == null) return new List<dynamic>();

            // En son batch'i al
            var latestBatch = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => h.OrderBatchId)
                .FirstOrDefaultAsync();

            if (latestBatch == Guid.Empty) return new List<dynamic>();

            // Ürün detaylarını al
            var items = await (from history in _dbContext.AddtionHistories
                               join product in _dbContext.Products on history.ProductName equals product.Name
                               where history.OrderBatchId == latestBatch
                               select new
                               {
                                   ProductName = history.ProductName,
                                   Quantity = history.ProductQuantity,
                                   Price = history.ProductPrice,
                                   ProductType = product.Type == ProductOrderType.Kitchen ? "Kitchen" : "Bar",
                                   CategoryName = product.CategoryName
                               }).ToListAsync();

            return items.Cast<dynamic>().ToList();
        }
        catch
        {
            return new List<dynamic>();
        }
    }
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