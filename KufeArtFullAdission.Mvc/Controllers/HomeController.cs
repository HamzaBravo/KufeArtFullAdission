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
            // Dashboard kartlar� i�in veriler
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
                var tables = await _dbContext.Tables
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Name)
                    .Select(x => new {
                        x.Id,
                        x.Name,
                        x.Category,
                        x.AddionStatus,
                        IsOccupied = x.AddionStatus.HasValue,
                        TotalAmount = x.AddionStatus.HasValue ?
                            _dbContext.AddtionHistories
                                .Where(h => h.AddionStatusId == x.AddionStatus)
                                .Sum(h => h.TotalPrice) : 0,
                        OpenedAt = x.AddionStatus.HasValue ?
                            _dbContext.AddtionHistories
                                .Where(h => h.AddionStatusId == x.AddionStatus)
                                .Min(h => h.CreatedAt) : (DateTime?)null
                    })
                    .ToListAsync();

                // Kategori bazl� grupla
                var groupedTables = tables
                    .GroupBy(x => x.Category)
                    .ToDictionary(g => g.Key, g => g.ToList());

                return Json(new { success = true, data = groupedTables });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTableDetails(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamad�!" });

                // Masa sipari�lerini getir (OrderBatchId bazl� gruplu)
                var orders = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == tableId)
                    .OrderByDescending(h => h.CreatedAt)
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
                        OrderBatchId = h.OrderBatchId // Batch ID'sini de ekle
                    })
                    .ToListAsync();

                var totalAmount = orders.Sum(o => o.TotalPrice);
                var isOccupied = orders.Any();

                var result = new TableDetailViewModel
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
                    TotalAmount = totalAmount
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Masa detaylar� al�namad�: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OpenTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamad�!" });

                if (table.AddionStatus.HasValue)
                    return Json(new { success = false, message = "Bu masada zaten a��k hesap var!" });

                // Yeni hesap ID'si olu�tur
                var newAccountId = Guid.NewGuid();

                // Masaya hesap ID'si ata
                table.AddionStatus = newAccountId;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap ba�ar�yla a��ld�!",
                    accountId = newAccountId,
                    tableId = tableId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata olu�tu: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CloseTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamad�!" });

                if (!table.AddionStatus.HasValue)
                    return Json(new { success = false, message = "Bu masada a��k hesap yok!" });

                // Hesap kapatma i�lemi
                table.AddionStatus = null;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap ba�ar�yla kapat�ld�!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata olu�tu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _dbContext.Products
                    .Where(p => p.IsActive) // Sadece aktif �r�nler
                    .OrderBy(p => p.CategoryName)
                    .ThenBy(p => p.Name)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        categoryName = p.CategoryName,
                        type = p.Type.ToString(), // Backend i�in (mutfak/bar y�nlendirme)
                        hasCampaign = p.HasCampaign,
                        campaignCaption = p.CampaignCaption
                    })
                    .ToListAsync();

                // Kategorilere g�re grupla
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
                return Json(new { success = false, message = "�r�nler y�klenemedi: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SubmitOrder([FromBody] OrderSubmissionDto orderDto)
        {
            try
            {
                if (orderDto?.Items == null || !orderDto.Items.Any())
                    return Json(new { success = false, message = "Sepet bo�!" });

                var table = await _dbContext.Tables.FindAsync(orderDto.TableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamad�!" });

                // Sipari� batch ID'si olu�tur (ayn� anda verilen sipari�ler i�in)
                var batchId = Guid.NewGuid();
                var currentUser = "Sistem"; // TODO: Login sisteminden gelecek
                var currentUserId = Guid.NewGuid(); // TODO: Login sisteminden gelecek

                // Her �r�n i�in sipari� kayd� olu�tur
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
                        PersonFullName = currentUser // Garson ad�
                    };

                    _dbContext.AddtionHistories.Add(orderHistory);
                }

                // E�er masa ilk defa a��l�yorsa AddionStatus ayarla
                if (table.AddionStatus == null)
                {
                    table.AddionStatus = batchId;
                }

                await _dbContext.SaveChangesAsync();

                // Ba�ar�l� response
                var totalAmount = orderDto.Items.Sum(i => i.Quantity * i.Price);
                return Json(new
                {
                    success = true,
                    message = $"Sipari� ba�ar�yla al�nd�! Toplam: ?{totalAmount:F2}",
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
                return Json(new { success = false, message = "Sipari� kaydedilemedi: " + ex.Message });
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
            // 1. �nce temel masa bilgilerini al
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

            // 2. Her masa i�in ayr� ayr� hesapla (Memory'de)
            foreach (var table in tables)
            {
                double totalAmount = 0;
                DateTime? openedAt = null;

                if (table.AddionStatus.HasValue)
                {
                    // Sipari�leri getir
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
                        // Sipari�i yoksa masa a��l�� zaman� olarak �imdiyi kullan
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