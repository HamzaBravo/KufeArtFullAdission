﻿using AppDbContext;
using Azure.Core;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.Mvc.Extensions;
using KufeArtFullAdission.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
                    .Where(h => h.AddionStatusId == table.AddionStatus && !h.IsCancelled)
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
        var currentUsername = User.GetFullName();

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
                    h.Id,
                    h.ProductName,
                    h.ProductQuantity,
                    h.ProductPrice,
                    h.TotalPrice,
                    h.ShorLabel,
                    h.CreatedAt,
                    h.PersonFullName,
                    h.IsCancelled,
                    h.CancelReason,
                    h.CancelledAt,
                    h.CancelledByName,
                    h.IsPaid,
                    h.PaidAt              // ✅ YENİ SATIR
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
                    personFullName = currentUsername // TODO: Person tablosundan join edilecek
                })
                .ToListAsync();

            // Hesaplamalar
            var totalOrderAmount = orders.Where(o => !o.IsCancelled).Sum(o => o.TotalPrice);
            var totalPaidAmount = payments.Sum(p => p.amount);
            var remainingAmount = Math.Max(0, totalOrderAmount - totalPaidAmount);


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

            // Masa durumunu sıfırla
            table.AddionStatus = null;

            await _dbContext.SaveChangesAsync();

            await NotifyWaitersTableClosed(table.Id, table.Name);

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
            var table = await _dbContext.Tables.FirstOrDefaultAsync(x => x.Id == paymentDto.TableId);
            if (table == null || !table.AddionStatus.HasValue)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            // ✅ YENİ: İptal edilmemiş siparişleri kontrol et
            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == paymentDto.AddionStatusId && !h.IsCancelled)
                .ToListAsync();

            if (!orders.Any())
                return Json(new { success = false, message = "Sipariş bulunamadı!" });

            // ✅ YENİ: Hangi ürünlerin ödeneceğini belirle
            List<AddtionHistoryDbEntity> selectedOrderItems = new();

            if (paymentDto.SelectedOrderItemIds?.Any() == true)
            {
                // Seçilen ürünler
                selectedOrderItems = orders
                    .Where(o => paymentDto.SelectedOrderItemIds.Contains(o.Id) && !o.IsPaid)
                    .ToList();
            }
            else
            {
                // Tüm ödenmemiş ürünler
                selectedOrderItems = orders.Where(o => !o.IsPaid).ToList();
            }

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
            await _dbContext.SaveChangesAsync(); // Payment ID'sini almak için

            // ✅ YENİ: Seçilen ürünleri ödendi olarak işaretle
            foreach (var orderItem in selectedOrderItems)
            {
                orderItem.IsPaid = true;
                orderItem.PaidAt = DateTime.Now;
                orderItem.PaymentId = payment.Id;
            }

            await _dbContext.SaveChangesAsync();
            await NotifyWaitersTableClosed(paymentDto.TableId, table.Name);

            return Json(new
            {
                success = true,
                message = "Ödeme başarıyla kaydedildi!",
                data = new
                {
                    paymentId = payment.Id,
                    paidOrderCount = selectedOrderItems.Count
                }
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
            Console.WriteLine($"🎯 ProcessQuickPayment başladı - Phone: {paymentDto.CustomerPhone}, UsePoints: {paymentDto.UseKufePoints}, RequestedPoints: {paymentDto.RequestedPoints}");

            var table = await _dbContext.Tables.FindAsync(paymentDto.TableId);
            if (table == null || !table.AddionStatus.HasValue)
                return Json(new { success = false, message = "Masa bulunamadı!" });

            var orders = await _dbContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .ToListAsync();

            if (!orders.Any())
                return Json(new { success = false, message = "Sipariş bulunamadı!" });

            // Mevcut ödemeler ve kalan tutar hesaplaması
            var existingPayments = await _dbContext.Payments
                .Where(p => p.AddionStatusId == table.AddionStatus)
                .SumAsync(p => p.Amount);

            var totalOrderAmount = orders.Sum(o => o.TotalPrice);
            var remainingAmount = totalOrderAmount - existingPayments;

            if (remainingAmount <= 0)
                return Json(new { success = false, message = "Bu hesap zaten tamamen ödenmiş!" });

            // Ödeme tutarı hesaplaması
            double paymentAmount = 0;
            List<AddtionHistoryDbEntity> selectedOrderItems = new();

            if (paymentDto.PaymentMode == "full")
            {
                paymentAmount = remainingAmount;
                selectedOrderItems = orders.Where(o => !o.IsPaid && !o.IsCancelled).ToList();
            }
            else if (paymentDto.PaymentMode == "partial")
            {
                paymentAmount = paymentDto.CustomAmount;

                // ✅ YENİ: Parçalı ödeme için FIFO mantığı ile ürünleri seç
                double remainingPayment = paymentAmount;
                selectedOrderItems = orders
                    .Where(o => !o.IsPaid && !o.IsCancelled)
                    .OrderBy(o => o.CreatedAt)
                    .TakeWhile(o =>
                    {
                        if (remainingPayment >= o.TotalPrice)
                        {
                            remainingPayment -= o.TotalPrice;
                            return true;
                        }
                        return false;
                    })
                    .ToList();
            }

            if (paymentAmount <= 0 || paymentAmount > remainingAmount)
                return Json(new { success = false, message = "Geçersiz ödeme tutarı!" });

            // 🎯 YENİ: KÜFE POINT İŞLEMLERİ
            Guid? customerId = null;
            int totalEarnedPoints = 0;
            int spentPoints = 0;
            double pointDiscountAmount = 0;
            string customerMessage = "";

            // Telefon numarası ile müşteriyi bul (OLUŞTURMA YOK)
            if (!string.IsNullOrEmpty(paymentDto.CustomerPhone))
            {
                Console.WriteLine($"🔍 Müşteri aranıyor: {paymentDto.CustomerPhone}");

                var customer = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == paymentDto.CustomerPhone && c.IsActive);

                if (customer == null)
                {
                    Console.WriteLine("❌ Müşteri bulunamadı - QR menüden kayıt olmalı");
                    // Müşteri yoksa sadece sipariş işlemi yapılır, puan işlemi YAPILMAZ
                    customerMessage = " | Müşteri kayıtlı değil (QR menüden kayıt olabilir)";
                }
                else
                {
                    Console.WriteLine($"✅ Müşteri bulundu: {customer.Fullname}");

                    // Mevcut müşteri ziyaret sayısını artır

                    customerId = customer.Id;

                    // Bu siparişten kazanılacak puanları hesapla
                    Console.WriteLine("🎁 Kazanılacak puanlar hesaplanıyor...");
                    foreach (var order in orders)
                    {
                        var product = await _dbContext.Products
                            .Where(p => p.Name == order.ProductName && p.IsActive)
                            .FirstOrDefaultAsync();

                        if (product != null && product.HasKufePoints && product.KufePoints > 0)
                        {
                            int orderPoints = product.KufePoints ?? 0 * order.ProductQuantity;
                            totalEarnedPoints += orderPoints;
                            Console.WriteLine($"➕ {product.Name}: {orderPoints} puan");
                        }
                    }
                    Console.WriteLine($"🎁 Toplam kazanılacak puan: {totalEarnedPoints}");

                    // Puan indirimi kullanılacak mı?
                    if (paymentDto.UseKufePoints && paymentDto.RequestedPoints > 0)
                    {
                        Console.WriteLine($"💰 Puan indirimi uygulanacak: {paymentDto.RequestedPoints} puan");

                        var customerPoints = await _dbContext.CustomerPoints
                            .FirstOrDefaultAsync(cp => cp.CustomerId == customerId);

                        if (customerPoints != null && customerPoints.TotalPoints >= paymentDto.RequestedPoints)
                        {
                            spentPoints = paymentDto.RequestedPoints;
                            pointDiscountAmount = spentPoints / 100.0; // 100 puan = 1 TL

                            Console.WriteLine($"💸 {spentPoints} puan harcanacak, ₺{pointDiscountAmount:F2} indirim");

                            // Ödeme tutarından düş
                            paymentAmount = Math.Max(0, paymentAmount - pointDiscountAmount);

                            // Puan bakiyesini güncelle
                            customerPoints.TotalPoints -= spentPoints;

                            // Puan harcama kaydı
                            var spentTransaction = new KufePointTransactionDbEntity
                            {
                                CustomerId = customerId.Value,
                                Type = PointType.Spent,
                                Points = -spentPoints,
                                Description = $"Ödeme indirimi - ₺{pointDiscountAmount:F2}"
                            };
                            await _dbContext.KufePointTransactions.AddAsync(spentTransaction);

                            customerMessage += $" | {spentPoints} puan kullandı (₺{pointDiscountAmount:F2})";
                        }
                        else
                        {
                            Console.WriteLine("⚠️ Yetersiz puan veya müşteri puan hesabı yok");
                            customerMessage += " | Yetersiz puan";
                        }
                    }

                    // Kazanılan puanları ekle (sadece kayıtlı müşteriler için)
                    if (totalEarnedPoints > 0)
                    {
                        Console.WriteLine($"➕ {totalEarnedPoints} puan hesaba ekleniyor");
                        await UpdateCustomerPoints(customerId.Value, table.AddionStatus, totalEarnedPoints, "Sipariş puanı");
                        customerMessage += $" | {totalEarnedPoints} puan kazandı";
                    }
                }
            }
            else
            {
                Console.WriteLine("📞 Telefon numarası girilmedi - Normal ödeme");
                customerMessage = " | Telefon numarası girilmedi";
            }

            Console.WriteLine($"💳 Final ödeme tutarı: ₺{paymentAmount:F2}");

            // Ödeme kaydı oluştur
            var payment = new PaymentDbEntity
            {
                TableId = paymentDto.TableId,
                AddionStatusId = table.AddionStatus.Value,
                PaymentType = paymentDto.PaymentType,
                Amount = paymentAmount,
                ShortLabel = paymentDto.PaymentLabel,
                PersonId = Guid.NewGuid() // TODO: Session'dan al
            };


            // ✅ YENİ: Seçilen ürünleri ödendi olarak işaretle
            foreach (var orderItem in selectedOrderItems)
            {
                orderItem.IsPaid = true;
                orderItem.PaidAt = DateTime.Now;
                orderItem.PaymentId = payment.Id;
            }

            _dbContext.Payments.Add(payment);



            // Hesap kapanış kontrolü
            var newTotalPaid = existingPayments + paymentAmount;
            var newRemainingAmount = Math.Max(0, totalOrderAmount - newTotalPaid);
            var shouldCloseAccount = paymentDto.PaymentMode == "full" || newRemainingAmount <= 0;

            if (shouldCloseAccount)
            {
                table.AddionStatus = null;
                Console.WriteLine("🔒 Hesap kapatıldı");
            }

            await _dbContext.SaveChangesAsync();

            // Mesaj oluştur
            string message = shouldCloseAccount
                ? $"Hesap kapatıldı! ₺{paymentAmount:F2}"
                : $"Parçalı ödeme alındı: ₺{paymentAmount:F2} - Kalan: ₺{newRemainingAmount:F2}";

            message += customerMessage;

            Console.WriteLine($"✅ İşlem tamamlandı: {message}");


            await NotifyWaitersTableClosed(table.Id, table.Name);

            return Json(new
            {
                success = true,
                message = message,
                data = new
                {
                    paidAmount = paymentAmount,
                    remainingAmount = newRemainingAmount,
                    accountClosed = shouldCloseAccount,
                    earnedPoints = totalEarnedPoints,
                    spentPoints = spentPoints,
                    discountAmount = pointDiscountAmount
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Hata: {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }


    private async Task UpdateCustomerPoints(Guid customerId, Guid? productId, int points, string description)
    {
        Console.WriteLine($"🎁 UpdateCustomerPoints: {customerId} için {points} puan");

        // Müşteri puan bakiyesi var mı kontrol et
        var customerPoints = await _dbContext.CustomerPoints
            .FirstOrDefaultAsync(cp => cp.CustomerId == customerId);

        if (customerPoints == null)
        {
            Console.WriteLine("➕ İlk puan hesabı oluşturuluyor");
            // İlk kez puan kazanıyor
            customerPoints = new CustomerPointsDbEntity
            {
                CustomerId = customerId,
                TotalPoints = points
            };
            await _dbContext.CustomerPoints.AddAsync(customerPoints);
        }
        else
        {
            Console.WriteLine($"➕ Mevcut puana ekleniyor: {customerPoints.TotalPoints} + {points}");
            // Mevcut puana ekle
            customerPoints.TotalPoints += points;
        }

        // Puan işlem kaydı
        var transaction = new KufePointTransactionDbEntity
        {
            ProductId = productId ?? Guid.Empty,
            CustomerId = customerId,
            Type = PointType.Earned,
            Points = points,
            Description = description
        };
        await _dbContext.KufePointTransactions.AddAsync(transaction);

        Console.WriteLine("✅ Puan işlemi kaydedildi");
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerPoints(string phoneNumber, Guid? tableId = null)
    {
        try
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return Json(new { success = false, message = "Telefon numarası gerekli!" });

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && c.IsActive);

            if (customer == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Bu telefon numarasına kayıtlı müşteri bulunamadı!"
                });
            }

            // Müşteri puan bakiyesini al
            var customerPoints = await _dbContext.CustomerPoints
                .FirstOrDefaultAsync(cp => cp.CustomerId == customer.Id);

            int currentPoints = customerPoints?.TotalPoints ?? 0;

            // 🎯 YENİ: Mevcut siparişlerden kazanılacak puanları hesapla
            int willEarnPoints = 0;

            if (tableId.HasValue)
            {
                // Masa bilgisini al
                var table = await _dbContext.Tables.FindAsync(tableId.Value);
                if (table?.AddionStatus != null)
                {
                    // Siparişleri al
                    var orders = await _dbContext.AddtionHistories
                        .Where(h => h.AddionStatusId == table.AddionStatus)
                        .ToListAsync();

                    // Her sipariş için puan hesapla
                    foreach (var order in orders)
                    {
                        var product = await _dbContext.Products
                            .Where(p => p.Name == order.ProductName && p.IsActive && p.HasKufePoints)
                            .FirstOrDefaultAsync();

                        if (product != null && product.KufePoints > 0)
                        {
                            willEarnPoints += product.KufePoints ?? 0 * order.ProductQuantity;
                        }
                    }
                }
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    customerId = customer.Id,
                    customerName = customer.Fullname,
                    phoneNumber = customer.PhoneNumber,
                    currentPoints = currentPoints,
                    willEarnPoints = willEarnPoints,
                    canUseDiscount = currentPoints >= 5000,
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Hata oluştu: " + ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderQuantity([FromBody] UpdateOrderQuantityRequest request)
    {
        try
        {
            var orderItem = await _dbContext.AddtionHistories.FindAsync(request.OrderItemId);

            if (orderItem == null)
                return Json(new { success = false, message = "Sipariş kalemi bulunamadı!" });

            if (orderItem.IsCancelled)
                return Json(new { success = false, message = "İptal edilmiş ürün güncellenemez!" });

            if (orderItem.IsPaid)
                return Json(new { success = false, message = "Ödenmiş ürün güncellenemez!" });

            if (request.NewQuantity <= 0)
            {
                // Ürünü tamamen kaldır
                orderItem.IsCancelled = true;
                orderItem.CancelReason = "Miktar güncelleme sırasında kaldırıldı";
                orderItem.CancelledAt = DateTime.Now;
                orderItem.CancelledByName = User.GetFullName();
            }
            else
            {
                // Miktarı güncelle
                var oldQuantity = orderItem.ProductQuantity;
                orderItem.ProductQuantity = request.NewQuantity;
                orderItem.TotalPrice = orderItem.ProductPrice * request.NewQuantity;

                Console.WriteLine($"📊 Miktar güncellendi: {orderItem.ProductName} - {oldQuantity} → {request.NewQuantity}");
            }

            await _dbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = request.NewQuantity <= 0 ? "Ürün kaldırıldı!" : "Miktar güncellendi!"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Hata: " + ex.Message });
        }
    }

    private async Task<double> GetDailySales(DateTime date)
    {
        return await _dbContext.AddtionHistories
          .Where(x => x.CreatedAt.Date == date && !x.IsCancelled)
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
            .Select(x => new
            {
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
                    .Where(h => h.AddionStatusId == table.AddionStatus && !h.IsCancelled) // ← DOĞRU SORGU
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

    private async Task NotifyWaitersTableClosed(Guid tableId, string tableName)
    {
        try
        {
            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>()
                .CreateClient("WaiterPanel");

            var notification = new
            {
                Type = "TableUpdate",
                TableId = tableId,
                TableName = tableName,
                Message = $"{tableName} hesabı kapatıldı"
            };

            await httpClient.PostAsJsonAsync("/api/waiter-notification", notification);
            Console.WriteLine($"✅ Garsonlara masa kapatma bildirimi gönderildi: {tableName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Garson bildirimi hatası: {ex.Message}");
        }
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

public class UpdateOrderQuantityRequest
{
    public Guid OrderItemId { get; set; }
    public int NewQuantity { get; set; }
}