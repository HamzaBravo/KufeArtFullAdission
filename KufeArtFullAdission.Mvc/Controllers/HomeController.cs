using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers
{
    public class HomeController(DBContext _dbContext) : Controller
    {

        public async Task<IActionResult> Index()
        {
            // Dashboard kartları için veriler
            var today = DateTime.Today;

            var dashboardData = new
            {
                DailySales = await GetDailySales(today),
                QrViewCount = await GetQrViewCount(today),
                ActiveTableCount = await GetActiveTableCount(),
                DailyOrderCount = await GetDailyOrderCount(today),
                Tables = await GetTablesWithStatus()
            };

            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                // Masaları kategorilere göre grupla
                var tables = await _dbContext.Tables
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                var groupedTables = new Dictionary<string, List<object>>();

                foreach (var table in tables)
                {
                    // Bu masa için siparişler var mı kontrol et
                    var orders = await _dbContext.AddtionHistories
                        .Where(h => h.AddionStatusId == table.Id)
                        .OrderBy(h => h.CreatedAt)
                        .ToListAsync();

                    var hasOrders = orders.Any();
                    var totalAmount = orders.Sum(o => o.TotalPrice);
                    var firstOrderTime = hasOrders ? orders.First().CreatedAt : (DateTime?)null;

                    var tableInfo = new
                    {
                        id = table.Id,
                        name = table.Name,
                        category = table.Category,
                        isOccupied = hasOrders,
                        totalAmount = totalAmount,
                        openedAt = firstOrderTime?.ToString("yyyy-MM-ddTHH:mm:ss") // Basit ISO format
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

        [HttpGet]
        public async Task<IActionResult> GetTableDetails(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamadı!" });

                // Masa siparişlerini getir
                var orders = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == tableId)
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new OrderInfo
                    {
                        Id = h.Id,
                        ShorLabel = h.ShorLabel,
                        ProductName = h.ProductName,
                        ProductPrice = h.ProductPrice,
                        ProductQuantity = h.ProductQuantity,
                        TotalPrice = h.TotalPrice,
                        PersonFullName = h.PersonFullName,
                        CreatedAt = h.CreatedAt,
                        OrderBatchId = h.OrderBatchId
                    })
                    .ToListAsync();

                // YENİ: Ödemeleri getir
                var payments = await _dbContext.Payments
                    .Where(p => p.AddionStatusId == tableId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PaymentInfo
                    {
                        Id = p.Id,
                        PaymentType = p.PaymentType,
                        Amount = p.Amount,
                        ShortLabel = p.ShortLabel,
                        CreatedAt = p.CreatedAt,
                        PersonFullName = "Garson" // TODO: Person tablosundan join edilecek
                    })
                    .ToListAsync();

                var totalOrderAmount = orders.Sum(o => o.TotalPrice);
                var totalPaidAmount = payments.Sum(p => p.Amount);
                var remainingAmount = totalOrderAmount - totalPaidAmount;
                var isOccupied = orders.Any();

                var result = new
                {
                    Table = new TableInfo
                    {
                        Id = table.Id,
                        Name = table.Name,
                        Category = table.Category,
                        AddionStatus = table.AddionStatus,
                        IsOccupied = isOccupied
                    },
                    Orders = orders,
                    Payments = payments, // YENİ
                    TotalOrderAmount = totalOrderAmount,
                    TotalPaidAmount = totalPaidAmount, // YENİ
                    RemainingAmount = remainingAmount, // YENİ
                    IsFullyPaid = remainingAmount <= 0 // YENİ
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Masa detayları alınamadı: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OpenTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamadı!" });

                if (table.AddionStatus.HasValue)
                    return Json(new { success = false, message = "Bu masada zaten açık hesap var!" });

                // Yeni hesap ID'si oluştur
                var newAccountId = Guid.NewGuid();

                // Masaya hesap ID'si ata
                table.AddionStatus = newAccountId;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap başarıyla açıldı!",
                    accountId = newAccountId,
                    tableId = tableId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CloseTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamadı!" });

                // Eski siparişleri sil
                var existingOrders = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == tableId)
                    .ToListAsync();

                _dbContext.AddtionHistories.RemoveRange(existingOrders);

                // Masa durumunu sıfırla
                table.AddionStatus = null;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap başarıyla kapatıldı!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hesap kapatılamadı: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _dbContext.Products
                    .Where(p => p.IsActive) // Sadece aktif ürünler
                    .OrderBy(p => p.CategoryName)
                    .ThenBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        categoryName = p.CategoryName,
                        type = p.Type.ToString(), // Backend için (mutfak/bar yönlendirme)
                        hasCampaign = p.HasCampaign,
                        campaignCaption = p.CampaignCaption
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

                return Json(new { success = true, data = new { categories = categories, allProducts = products } });
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

                // Sipariş batch ID'si oluştur (aynı anda verilen siparişler için)
                var batchId = Guid.NewGuid();
                var currentUser = "Sistem"; // TODO: Login sisteminden gelecek
                var currentUserId = Guid.NewGuid(); // TODO: Login sisteminden gelecek

                // Her ürün için sipariş kaydı oluştur
                foreach (var item in orderDto.Items)
                {
                    var product = await _dbContext.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    var orderHistory = new AddtionHistoryDbEntity
                    {
                        AddionStatusId = table.Id, // Masa ID'si
                        OrderBatchId = batchId, // Yeni eklenen field
                        ShorLabel = orderDto.WaiterNote, // Garson notu
                        ProductName = product.Name,
                        ProductPrice = product.Price,
                        ProductQuantity = item.Quantity,
                        TotalPrice = product.Price * item.Quantity,
                        PersonId = currentUserId, // Garson ID'si
                        PersonFullName = currentUser // Garson adı
                    };

                    _dbContext.AddtionHistories.Add(orderHistory);
                }

                // Eğer masa ilk defa açılıyorsa AddionStatus ayarla
                if (table.AddionStatus == null)
                {
                    table.AddionStatus = batchId;
                }

                await _dbContext.SaveChangesAsync();

                // Başarılı response
                var totalAmount = orderDto.Items.Sum(i => i.Quantity * i.Price);
                return Json(new
                {
                    success = true,
                    message = $"Sipariş başarıyla alındı! Toplam: ₺{totalAmount:F2}",
                    data = new
                    {
                        batchId = batchId,
                        tableId = table.Id,
                        totalAmount = totalAmount,
                        itemCount = orderDto.Items.Sum(i => i.Quantity)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Sipariş kaydedilemedi: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentDto paymentDto)
        {
            try
            {
                var payment = new PaymentDbEntity
                {
                    TableId = paymentDto.TableId,
                    AddionStatusId = paymentDto.AddionStatusId,
                    PaymentType = paymentDto.PaymentType,
                    Amount = paymentDto.Amount,
                    ShortLabel = paymentDto.ShortLabel,
                    PersonId = paymentDto.PersonId // TODO: Session'dan al
                };

                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Ödeme başarıyla kaydedildi!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<double> GetDailySales(DateTime date)
        {
            return await _dbContext.AddtionHistories
                .Where(x => x.CreatedAt.Date == date)
                .SumAsync(x => x.TotalPrice);
        }

        private async Task<int> GetQrViewCount(DateTime date)
        {
            return await _dbContext.QrMenuVisibles
                .CountAsync(x => x.CreatedAt.Date == date);
        }

        private async Task<int> GetActiveTableCount()
        {
            return await _dbContext.Tables
                .CountAsync(x => x.IsActive && x.AddionStatus.HasValue);
        }

        private async Task<int> GetDailyOrderCount(DateTime date)
        {
            return await _dbContext.AddtionHistories
                .CountAsync(x => x.CreatedAt.Date == date);
        }

        private async Task<object> GetTablesWithStatus()
        {
            // 1. Önce temel masa bilgilerini al
            var tables = await _dbContext.Tables
                .Where(x => x.IsActive)
                .Select(x => new {
                    x.Id,
                    x.Name,
                    x.Category,
                    x.AddionStatus,
                    IsOccupied = x.AddionStatus.HasValue
                })
                .ToListAsync();

            var result = new List<object>();

            // 2. Her masa için ayrı ayrı hesapla (Memory'de)
            foreach (var table in tables)
            {
                double totalAmount = 0;
                DateTime? openedAt = null;

                if (table.AddionStatus.HasValue)
                {
                    // Siparişleri getir
                    var orders = await _dbContext.AddtionHistories
                        .Where(h => h.AddionStatusId == table.AddionStatus)
                        .Select(h => new { h.TotalPrice, h.CreatedAt })
                        .ToListAsync();

                    if (orders.Any())
                    {
                        totalAmount = orders.Sum(x => x.TotalPrice);
                        openedAt = orders.Min(x => x.CreatedAt);
                    }
                    else
                    {
                        // Siparişi yoksa masa açılış zamanı olarak şimdiyi kullan
                        totalAmount = 0;
                        openedAt = DateTime.Now;
                    }
                }

                result.Add(new
                {
                    table.Id,
                    table.Name,
                    table.Category,
                    table.AddionStatus,
                    IsOccupied = table.IsOccupied,
                    TotalAmount = totalAmount,
                    OpenedAt = openedAt
                });
            }

            return result;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}