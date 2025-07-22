using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

[Authorize]
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
                    .Where(h => h.AddionStatusId == table.AddionStatus)
                    .OrderBy(h => h.CreatedAt)
                    .ToListAsync();

                var hasOrders = orders.Any();
                var totalOrderAmount = orders.Sum(o => o.TotalPrice);

                // ✅ ÖDEMELERİ DE HESAPLA
                var totalPaidAmount = 0.0;
                if (table.AddionStatus.HasValue)
                {
                    totalPaidAmount = await _dbContext.Payments
                        .Where(p => p.AddionStatusId == table.AddionStatus)
                        .SumAsync(p => p.Amount);
                }

                // ✅ KALAN TUTARI HESAPLA
                var remainingAmount = Math.Max(0, totalOrderAmount - totalPaidAmount);
                var firstOrderTime = hasOrders ? orders.First().CreatedAt : (DateTime?)null;

                var tableInfo = new
                {
                    id = table.Id,
                    name = table.Name,
                    category = table.Category,
                    isOccupied = hasOrders,
                    totalAmount = remainingAmount, // ✅ Kalan tutarı göster (eski: totalOrderAmount)
                    totalOrderAmount = totalOrderAmount, // ✅ Toplam sipariş tutarı
                    totalPaidAmount = totalPaidAmount,   // ✅ Ödenen tutar
                    remainingAmount = remainingAmount,    // ✅ Kalan tutar
                    openedAt = firstOrderTime?.ToString("yyyy-MM-ddTHH:mm:ss")
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

            // Boş masa kontrolü - AddionStatus'a göre
            var isOccupied = table.AddionStatus.HasValue;

            if (!isOccupied)
            {
                // Boş masa için özel response
                var emptyTableResult = new
                {
                    table = new
                    {
                        id = table.Id,
                        name = table.Name,
                        category = table.Category,
                        addionStatus = table.AddionStatus,
                        isOccupied = false
                    },
                    orders = new List<object>(),
                    payments = new List<object>(),
                    totalOrderAmount = 0.0,
                    totalPaidAmount = 0.0,
                    remainingAmount = 0.0,
                    isFullyPaid = true
                };

                return Json(new { success = true, data = emptyTableResult });
            }

            // Dolu masa için: Siparişleri getir (AddionStatus ile)
            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new
                {
                    id = h.Id,
                    shorLabel = h.ShorLabel,
                    productName = h.ProductName,
                    productPrice = h.ProductPrice,
                    productQuantity = h.ProductQuantity,
                    totalPrice = h.TotalPrice,
                    personFullName = h.PersonFullName,
                    createdAt = h.CreatedAt,
                    orderBatchId = h.OrderBatchId
                })
                .ToListAsync();

            // Ödemeleri getir (AddionStatus ile)
            var payments = await _dbContext.Payments
                .Where(p => p.AddionStatusId == table.AddionStatus)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id,
                    paymentType = (int)p.PaymentType,
                    amount = p.Amount,
                    shortLabel = p.ShortLabel,
                    createdAt = p.CreatedAt,
                    personFullName = "Garson" // TODO: Person tablosundan join edilecek
                })
                .ToListAsync();

            // Hesaplamalar
            var totalOrderAmount = orders.Sum(o => o.totalPrice);
            var totalPaidAmount = payments.Sum(p => p.amount);
            var remainingAmount = Math.Max(0, totalOrderAmount - totalPaidAmount); // ✅ Negatif değer engelle


            var result = new
            {
                table = new
                {
                    id = table.Id,
                    name = table.Name,
                    category = table.Category,
                    addionStatus = table.AddionStatus,
                    isOccupied = true
                },
                orders = orders,
                payments = payments,
                totalOrderAmount = totalOrderAmount,
                totalPaidAmount = totalPaidAmount,
                remainingAmount = remainingAmount,
                isFullyPaid = remainingAmount <= 0.01
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

            // 🎯 DOĞRU MANTIK: AddionStatus kontrolü
            Guid addionStatusId;

            if (table.AddionStatus == null)
            {
                // İlk sipariş - Yeni AddionStatus oluştur
                addionStatusId = Guid.NewGuid();
                table.AddionStatus = addionStatusId;
                System.Diagnostics.Debug.WriteLine($"✅ Yeni AddionStatus oluşturuldu: {addionStatusId}");
            }
            else
            {
                // Mevcut sipariş - Var olan AddionStatus'u kullan
                addionStatusId = table.AddionStatus.Value;
                System.Diagnostics.Debug.WriteLine($"✅ Mevcut AddionStatus kullanılıyor: {addionStatusId}");
            }

            // Sipariş batch ID'si oluştur
            var batchId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid(); // TODO: Login sisteminden gelecek
            var currentUser = "Garson"; // TODO: Login sisteminden gelecek

            // Her ürün için sipariş kaydı oluştur
            foreach (var item in orderDto.Items)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                var orderHistory = new AddtionHistoryDbEntity
                {
                    AddionStatusId = addionStatusId, // ← DOĞRU: AddionStatus kullan
                    OrderBatchId = batchId,
                    ShorLabel = orderDto.WaiterNote,
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

            var totalAmount = orderDto.Items.Sum(i => i.Quantity * i.Price);
            return Json(new
            {
                success = true,
                message = $"Sipariş başarıyla alındı! Toplam: ₺{totalAmount:F2}",
                data = new
                {
                    addionStatusId = addionStatusId,
                    tableId = table.Id,
                    totalAmount = totalAmount
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


    [HttpPost]
    public async Task<IActionResult> ProcessQuickPayment([FromBody] QuickPaymentDto paymentDto)
    {
        try
        {
            var table = await _dbContext.Tables.FindAsync(paymentDto.TableId);
            if (table == null || !table.AddionStatus.HasValue)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .ToListAsync();

            if (!orders.Any())
                return Json(new { success = false, message = "Sipariş bulunamadı!" });

            // ✅ MEVCUT ÖDEMELERİ HESAPLA
            var existingPayments = await _dbContext.Payments
                .Where(p => p.AddionStatusId == table.AddionStatus)
                .SumAsync(p => p.Amount);

            var totalOrderAmount = orders.Sum(o => o.TotalPrice);
            var remainingAmount = totalOrderAmount - existingPayments;

            // ✅ FAZLA ÖDEME KONTROLÜ
            if (remainingAmount <= 0)
            {
                return Json(new { success = false, message = "Bu hesap zaten tamamen ödenmiş!" });
            }

            double paymentAmount = 0;

            if (paymentDto.PaymentMode == "full")
            {
                paymentAmount = remainingAmount; // ✅ Kalan tutarı öde (fazlasını değil)
            }
            else if (paymentDto.PaymentMode == "partial")
            {
                paymentAmount = paymentDto.CustomAmount;

                // ✅ PARÇALI ÖDEME KONTROLÜ
                if (paymentAmount > remainingAmount)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Ödeme tutarı (₺{paymentAmount:F2}) kalan borcu (₺{remainingAmount:F2}) aşıyor!"
                    });
                }
            }

            // ✅ GÜVENLİK KONTROLÜ
            if (paymentAmount <= 0)
            {
                return Json(new { success = false, message = "Ödeme tutarı sıfırdan büyük olmalıdır!" });
            }

            // Ödeme kaydı oluştur
            var payment = new PaymentDbEntity
            {
                TableId = paymentDto.TableId,
                AddionStatusId = table.AddionStatus.Value,
                PaymentType = paymentDto.PaymentType,
                Amount = paymentAmount,
                ShortLabel = paymentDto.PaymentLabel,
                PersonId = Guid.NewGuid()
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            // Yeni kalan tutarı hesapla
            var newTotalPaid = existingPayments + paymentAmount;
            var newRemainingAmount = Math.Max(0, totalOrderAmount - newTotalPaid);
            var isFullyPaid = newRemainingAmount <= 0.01; // Küsurat toleransı

            var shouldCloseAccount = paymentDto.PaymentMode == "full" || isFullyPaid;

            if (shouldCloseAccount)
            {
                _dbContext.AddtionHistories.RemoveRange(orders);
                table.AddionStatus = null;
                await _dbContext.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                message = shouldCloseAccount
                    ? $"Hesap kapatıldı! ₺{paymentAmount:F2}"
                    : $"Parçalı ödeme alındı: ₺{paymentAmount:F2} - Kalan: ₺{newRemainingAmount:F2}",
                data = new
                {
                    paidAmount = paymentAmount,
                    remainingAmount = newRemainingAmount,
                    accountClosed = shouldCloseAccount
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Ödeme tutarını hesapla
    private double CalculatePaymentAmount(List<AddtionHistoryDbEntity> orders, QuickPaymentDto paymentDto)
    {
        var totalAmount = orders.Sum(o => o.TotalPrice);

        return paymentDto.PaymentMode switch
        {
            "full" => totalAmount,
            "half" => Math.Round(totalAmount / 2, 2),
            "tip15" => Math.Round(totalAmount * 1.15, 2),
            "tip10" => Math.Round(totalAmount * 1.10, 2),
            "tip20" => Math.Round(totalAmount * 1.20, 2),
            "label" => orders
                .Where(o => o.ShorLabel == paymentDto.PaymentLabel)
                .Sum(o => o.TotalPrice),
            "custom" => paymentDto.CustomAmount,
            _ => totalAmount
        };
    }

    // Ödeme mesajı oluştur
    private string GetPaymentMessage(string mode, double amount)
    {
        return mode switch
        {
            "full" => $"Hesap tamamen kapatıldı! Toplam: ₺{amount:F2}",
            "half" => $"Yarım ödeme alındı: ₺{amount:F2}",
            "tip15" => $"₺{amount:F2} (+%15 bahşiş ile) ödeme alındı!",
            "tip10" => $"₺{amount:F2} (+%10 bahşiş ile) ödeme alındı!",
            "tip20" => $"₺{amount:F2} (+%20 bahşiş ile) ödeme alındı!",
            "label" => $"Etiket ödemesi alındı: ₺{amount:F2}",
            "custom" => $"Özel tutar ödemesi alındı: ₺{amount:F2}",
            _ => $"Ödeme alındı: ₺{amount:F2}"
        };
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
        // 1. Masa bilgilerini al
        var tables = await _dbContext.Tables
            .Where(x => x.IsActive)
            .Select(x => new {
                x.Id,
                x.Name,
                x.Category,
                x.AddionStatus,
                IsOccupied = x.AddionStatus.HasValue // ← DOĞRU KONTROL
            })
            .ToListAsync();

        var result = new List<object>();

        // 2. Her masa için hesaplama yap
        foreach (var table in tables)
        {
            double totalAmount = 0;
            DateTime? openedAt = null;

            if (table.AddionStatus.HasValue) // ← AddionStatus varsa dolu
            {
                // Siparişleri AddionStatus ile getir
                var orders = await _dbContext.AddtionHistories
                    .Where(h => h.AddionStatusId == table.AddionStatus) // ← DOĞRU SORGU
                    .Select(h => new { h.TotalPrice, h.CreatedAt })
                    .ToListAsync();

                if (orders.Any())
                {
                    totalAmount = orders.Sum(x => x.TotalPrice);
                    openedAt = orders.Min(x => x.CreatedAt);
                }
            }

            result.Add(new
            {
                table.Id,
                table.Name,
                table.Category,
                table.AddionStatus,
                IsOccupied = table.IsOccupied, // AddionStatus.HasValue
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