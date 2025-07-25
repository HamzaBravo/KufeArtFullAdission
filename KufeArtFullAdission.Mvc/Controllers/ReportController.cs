﻿using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

[Authorize]
public class ReportController(DBContext _dbContext) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // 🎯 Personel Performans Raporu
    public async Task<IActionResult> StaffPerformance(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Varsayılan: Son 30 gün
        startDate ??= DateTime.Today.AddDays(-30);
        endDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);

        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

        var staffPerformance = await GetStaffPerformanceData(startDate.Value, endDate.Value);

        return View(staffPerformance);
    }

    // 🎯 Ciro Analizi
    public async Task<IActionResult> SalesAnalysis(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Today.AddDays(-7);
        endDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);

        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

        var salesData = await GetSalesAnalysisData(startDate.Value, endDate.Value);

        return View(salesData);
    }

    // 🎯 Masa Performans Analizi
    public async Task<IActionResult> TableHeatmap(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.Today.AddDays(-30);
        endDate ??= DateTime.Today.AddDays(1).AddSeconds(-1);

        ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

        var tableData = await GetTableHeatmapData(startDate.Value, endDate.Value);

        return View(tableData);
    }

    #region API Endpoints for Charts

    [HttpGet]
    public async Task<IActionResult> GetStaffPerformanceApi(DateTime startDate, DateTime endDate)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"API called with dates: {startDate} - {endDate}");

            var data = await GetStaffPerformanceData(startDate, endDate);

            // ✅ JSON serializasyon test
            var jsonResult = Json(new { success = true, data });

            System.Diagnostics.Debug.WriteLine($"JSON Result created successfully");

            return jsonResult;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            return Json(new
            {
                success = false,
                message = ex.Message,
                details = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSalesAnalysisApi(DateTime startDate, DateTime endDate)
    {
        try
        {
            var data = await GetSalesAnalysisData(startDate, endDate);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Controllers/ReportController.cs - Bu API endpoint'ini ekleyin

    [HttpGet]
    public async Task<IActionResult> GetTableHeatmapApi(DateTime startDate, DateTime endDate)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Heatmap API called with dates: {startDate} - {endDate}");

            var data = await GetTableHeatmapData(startDate, endDate);

            System.Diagnostics.Debug.WriteLine($"Heatmap API - data created successfully");

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Heatmap API Error: {ex.Message}");
            return Json(new
            {
                success = false,
                message = ex.Message,
                details = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))
            });
        }
    }

    #endregion

    #region Private Methods


    private async Task<object> GetStaffPerformanceData(DateTime startDate, DateTime endDate)
    {
        try
        {
            startDate = startDate.Date.AddSeconds(-1);
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            // ✅ GÜNCEL: Ürün prim bilgileriyle birlikte veri çek
            var rawOrderData = await (from h in _dbContext.AddtionHistories
                                      join p in _dbContext.Products on h.ProductName equals p.Name into productJoin
                                      from product in productJoin.DefaultIfEmpty()
                                      where h.CreatedAt >= startDate && h.CreatedAt <= endDate
                                      select new
                                      {
                                          h.PersonId,
                                          h.PersonFullName,
                                          h.TotalPrice,
                                          h.ProductName,
                                          h.ProductQuantity,
                                          h.AddionStatusId,
                                          h.CreatedAt,
                                          // ✅ YENİ: Prim bilgileri
                                          IsCommissionEligible = product != null ? product.IsCommissionEligible : true,
                                          CommissionRate = product != null ? product.CommissionRate : 5.0
                                      })
                                      .ToListAsync();

            if (!rawOrderData.Any())
            {
                return new
                {
                    StaffSummary = new object[0],
                    DailyTrends = new object[0],
                    TopProducts = new object[0],
                    PeriodStart = startDate.ToString("yyyy-MM-dd"),
                    PeriodEnd = endDate.ToString("yyyy-MM-dd"),
                    TotalStaff = 0,
                    TotalRevenue = 0.0,
                    AvgOrdersPerStaff = 0.0
                };
            }

            // ✅ GÜNCEL: Prim hesaplama ile personel özetleri
            var staffOrders = rawOrderData
                .GroupBy(h => new { h.PersonId, h.PersonFullName })
                .Select(g => new
                {
                    PersonId = g.Key.PersonId,
                    PersonName = g.Key.PersonFullName,
                    TotalOrders = g.Count(),
                    TotalSales = g.Sum(h => h.TotalPrice),
                    AverageOrderValue = g.Average(h => h.TotalPrice),
                    TopSellingProduct = g.GroupBy(h => h.ProductName)
                                       .OrderByDescending(pg => pg.Sum(h => h.TotalPrice))
                                       .Select(pg => pg.Key)
                                       .FirstOrDefault() ?? "Veri yok",
                    UniqueCustomers = g.Select(h => h.AddionStatusId).Distinct().Count(),
                    // ✅ YENİ: Ürün bazlı prim hesaplama
                    TotalCommission = g.Where(h => h.IsCommissionEligible)
                                      .Sum(h => h.TotalPrice * (h.CommissionRate / 100.0)),
                    CommissionEligibleSales = g.Where(h => h.IsCommissionEligible).Sum(h => h.TotalPrice),
                    NonCommissionSales = g.Where(h => !h.IsCommissionEligible).Sum(h => h.TotalPrice)
                })
                .OrderByDescending(s => s.TotalSales)
                .ToList();

            // Geri kalan kod aynı...
            var dailyTrends = rawOrderData
                .GroupBy(h => new {
                    Date = h.CreatedAt.Date,
                    h.PersonId,
                    h.PersonFullName
                })
                .Select(g => new
                {
                    Date = g.Key.Date.ToString("yyyy-MM-dd"),
                    PersonId = g.Key.PersonId,
                    PersonName = g.Key.PersonFullName,
                    DailySales = g.Sum(h => h.TotalPrice),
                    DailyOrders = g.Count(),
                    // ✅ YENİ: Günlük prim
                    DailyCommission = g.Where(h => h.IsCommissionEligible)
                                      .Sum(h => h.TotalPrice * (h.CommissionRate / 100.0))
                })
                .OrderBy(d => d.Date)
                .ToList();

            var result = new
            {
                StaffSummary = staffOrders,
                DailyTrends = dailyTrends,
                TopProducts = new object[0], // Şimdilik boş
                PeriodStart = startDate.ToString("yyyy-MM-dd"),
                PeriodEnd = endDate.ToString("yyyy-MM-dd"),
                TotalStaff = staffOrders.Count,
                TotalRevenue = staffOrders.Sum(s => s.TotalSales),
                AvgOrdersPerStaff = staffOrders.Any() ? staffOrders.Average(s => s.TotalOrders) : 0,
                // ✅ YENİ: Toplam prim bilgisi
                TotalCommissionPaid = staffOrders.Sum(s => s.TotalCommission)
            };

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetStaffPerformanceData Error: {ex.Message}");
            throw;
        }
    }

    // Diğer metodlar için de aynı tekniği uygulayalım
    // Controllers/ReportController.cs - GetSalesAnalysisData metodunu düzelt

    private async Task<object> GetSalesAnalysisData(DateTime startDate, DateTime endDate)
    {
        startDate= startDate.Date.AddSeconds(-1); // Başlangıç tarihini bir gün öncesine al
        endDate = endDate.Date.AddDays(1).AddSeconds(-1); // Bitiş tarihini bir gün sonrasına al

        try
        {
            // 🔥 Önce raw data çek
            var orderData = await _dbContext.AddtionHistories
                .Where(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate)
                .Select(h => new
                {
                    h.TotalPrice,
                    h.ProductName,
                    h.ProductQuantity,
                    h.ProductPrice,
                    h.AddionStatusId,
                    h.CreatedAt
                })
                .ToListAsync();

            // Products tablosundan kategori bilgilerini al
            var productCategories = await _dbContext.Products
                .Select(p => new { p.Name, p.CategoryName })
                .ToListAsync();

            // 🔥 Memory'de grupla
            var dailySales = orderData
                .GroupBy(h => h.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"), // ✅ küçük harf
                    totalSales = g.Sum(h => h.TotalPrice), // ✅ küçük harf
                    orderCount = g.Count(), // ✅ küçük harf
                    avgOrderValue = g.Average(h => h.TotalPrice), // ✅ camelCase
                    uniqueCustomers = g.Select(h => h.AddionStatusId).Distinct().Count() // ✅ camelCase
                })
                .OrderBy(d => d.date)
                .ToList();

            // Kategori breakdown (JOIN ile)
            var categoryBreakdown = orderData
                .Join(productCategories,
                      o => o.ProductName,
                      p => p.Name,
                      (o, p) => new { Order = o, Category = p.CategoryName })
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    category = g.Key ?? "Diğer", // ✅ küçük harf
                    totalSales = g.Sum(x => x.Order.TotalPrice), // ✅ küçük harf
                    quantity = g.Sum(x => x.Order.ProductQuantity), // ✅ küçük harf
                    orderCount = g.Count(), // ✅ küçük harf
                    avgPrice = g.Average(x => x.Order.ProductPrice) // ✅ camelCase
                })
                .OrderByDescending(c => c.totalSales)
                .ToList();

            // En çok Satan ürünler
            var topProducts = orderData
                .GroupBy(h => h.ProductName)
                .Select(g => new
                {
                    productName = g.Key, // ✅ camelCase
                    totalSales = g.Sum(h => h.TotalPrice), // ✅ küçük harf
                    quantity = g.Sum(h => h.ProductQuantity), // ✅ küçük harf
                    orderCount = g.Count(), // ✅ küçük harf
                    avgPrice = g.Average(h => h.ProductPrice) // ✅ camelCase
                })
                .OrderByDescending(p => p.totalSales)
                .Take(10)
                .ToList();

            // Saatlik satış
            var hourlySales = orderData
                .GroupBy(h => h.CreatedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key, // ✅ küçük harf
                    totalSales = g.Sum(h => h.TotalPrice), // ✅ küçük harf
                    orderCount = g.Count() // ✅ küçük harf
                })
                .OrderBy(h => h.hour)
                .ToList();

            var result = new
            {
                dailySales = dailySales, // ✅ camelCase
                categoryBreakdown = categoryBreakdown, // ✅ camelCase
                topProducts = topProducts, // ✅ camelCase
                hourlySales = hourlySales, // ✅ camelCase
                summary = new // ✅ küçük harf
                {
                    totalRevenue = dailySales.Sum(d => d.totalSales), // ✅ camelCase
                    totalOrders = dailySales.Sum(d => d.orderCount), // ✅ camelCase
                    avgDailyRevenue = dailySales.Any() ? dailySales.Average(d => d.totalSales) : 0, // ✅ camelCase
                    bestDay = dailySales.OrderByDescending(d => d.totalSales).FirstOrDefault(), // ✅ camelCase
                    peakHour = hourlySales.OrderByDescending(h => h.totalSales).FirstOrDefault() // ✅ camelCase
                }
            };

            System.Diagnostics.Debug.WriteLine($"Sales Analysis - Daily Sales Count: {result.dailySales.Count}");
            System.Diagnostics.Debug.WriteLine($"Sales Analysis - Total Revenue: {result.summary.totalRevenue}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetSalesAnalysisData Error: {ex.Message}");
            throw;
        }
    }

    private async Task<object> GetTableHeatmapData(DateTime startDate, DateTime endDate)
    {
        try
        {
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            System.Diagnostics.Debug.WriteLine($"🔍 Heatmap query date range: {startDate} - {endDate}");

            // Raw veri kontrolü
            var rawTableData = await (from h in _dbContext.AddtionHistories
                                      join t in _dbContext.Tables on h.TableId equals t.Id
                                      where h.CreatedAt >= startDate && h.CreatedAt <= endDate && t.IsActive
                                      select new
                                      {
                                          TableId = t.Id,
                                          TableName = t.Name,
                                          Category = t.Category,
                                          TotalPrice = h.TotalPrice,
                                          AddionStatusId = h.AddionStatusId,
                                          CreatedAt = h.CreatedAt
                                      })
                                     .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"🔍 Raw table data count: {rawTableData.Count}");

            // Günlük masa kullanımı - DEBUG eklendi
            var dailyTableUsage = rawTableData
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    activeTables = g.Select(x => x.TableId).Distinct().Count(),
                    totalOrders = g.Count(),
                    totalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderBy(d => d.date)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"🔍 Daily table usage count: {dailyTableUsage.Count}");
            dailyTableUsage.ForEach(d => System.Diagnostics.Debug.WriteLine($"   Date: {d.date}, Tables: {d.activeTables}, Orders: {d.totalOrders}"));

            // Saatlik masa yoğunluğu - DEBUG eklendi
            var timeSlotAnalysis = rawTableData
                .GroupBy(x => x.CreatedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    totalActivity = g.Count(),
                    activeTables = g.Select(x => x.TableId).Distinct().Count(),
                    avgRevenue = g.Average(x => x.TotalPrice),
                    totalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderBy(h => h.hour)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"🔍 Time slot analysis count: {timeSlotAnalysis.Count}");
            timeSlotAnalysis.ForEach(t => System.Diagnostics.Debug.WriteLine($"   Hour: {t.hour}, Activity: {t.totalActivity}, Tables: {t.activeTables}"));

            // Diğer hesaplamalar...
            var allTables = await _dbContext.Tables
                .Where(t => t.IsActive)
                .Select(t => new { t.Id, t.Name, t.Category })
                .ToListAsync();

            // tableStats hesaplaması...
            var allTableStats = allTables.Select(table =>
            {
                var hasData = rawTableData.Any(r => r.TableId == table.Id);

                if (hasData)
                {
                    var tableData = rawTableData.Where(r => r.TableId == table.Id).ToList();
                    var totalRevenue = tableData.Sum(r => r.TotalPrice);
                    var orderCount = tableData.Count();
                    var uniqueCustomers = tableData.Select(r => r.AddionStatusId).Distinct().Count();
                    var avgOrderValue = tableData.Average(r => r.TotalPrice);

                    return new
                    {
                        tableId = table.Id,
                        tableName = table.Name,
                        category = table.Category,
                        totalRevenue = totalRevenue,
                        orderCount = orderCount,
                        uniqueCustomers = uniqueCustomers,
                        avgOrderValue = avgOrderValue,
                        avgSessionDuration = 30.0, // Basitleştirildi
                        isUsed = true,
                        performanceScore = CalculatePerformanceScore(totalRevenue, orderCount, 30.0),
                        suggestions = GenerateTableSuggestions(table.Name, totalRevenue, orderCount, 30.0, true)
                    };
                }
                else
                {
                    return new
                    {
                        tableId = table.Id,
                        tableName = table.Name,
                        category = table.Category,
                        totalRevenue = 0.0,
                        orderCount = 0,
                        uniqueCustomers = 0,
                        avgOrderValue = 0.0,
                        avgSessionDuration = 0.0,
                        isUsed = false,
                        performanceScore = 0,
                        suggestions = GenerateTableSuggestions(table.Name, 0, 0, 0, false)
                    };
                }
            })
            .OrderByDescending(t => t.performanceScore)
            .ThenByDescending(t => t.totalRevenue)
            .ToList();

            var result = new
            {
                tableStats = allTableStats,
                categoryPerformance = new List<object>(), // Geçici olarak boş
                dailyOccupancy = dailyTableUsage, // ✅ Bu isim çok önemli!
                timeSlotAnalysis = timeSlotAnalysis, // ✅ Bu isim çok önemli!
                summary = new
                {
                    totalTables = allTables.Count,
                    usedTables = allTableStats.Count(t => t.isUsed),
                    unusedTables = allTableStats.Count(t => !t.isUsed),
                    utilizationRate = allTables.Count > 0 ? (double)allTableStats.Count(t => t.isUsed) / allTables.Count * 100 : 0
                }
            };

            System.Diagnostics.Debug.WriteLine($"🔍 Final result - Daily occupancy: {result.dailyOccupancy.Count()}, Time slots: {result.timeSlotAnalysis.Count()}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ GetTableHeatmapData Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    // ✅ 3. ADİM: Yardımcı metod - Doğru tip ile
    private double CalculateAvgSessionDuration<T>(List<T> orderData) where T : class
    {
        try
        {
            // ✅ Generic type ile çalış
            var sessions = orderData
                .GroupBy(x => GetProperty(x, "AddionStatusId"))
                .Where(session => session.Count() > 0)
                .Select(session =>
                {
                    var firstOrder = session.Min(s => (DateTime)GetProperty(s, "CreatedAt"));
                    var lastOrder = session.Max(s => (DateTime)GetProperty(s, "CreatedAt"));
                    return (lastOrder - firstOrder).TotalMinutes;
                })
                .Where(duration => duration >= 0)
                .ToList();

            return sessions.Any() ? sessions.Average() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private int CalculatePerformanceScore(double revenue, int orderCount, double sessionDuration)
    {
        if (revenue == 0) return 0;

        var revenueScore = Math.Min(revenue / 10, 50); // Maksimum 50 puan
        var orderScore = Math.Min(orderCount * 2, 30);  // Maksimum 30 puan
        var durationScore = sessionDuration > 60 ? 20 : Math.Max(sessionDuration / 3, 5); // Maksimum 20 puan

        return (int)(revenueScore + orderScore + durationScore);
    }

    // ✅ Property değerini alma helper metodu
    private object GetProperty(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    private List<string> GenerateTableSuggestions(string tableName, double revenue, int orderCount, double sessionDuration, bool isUsed)
    {
        var suggestions = new List<string>();

        if (!isUsed)
        {
            suggestions.Add("🚨 Bu masa hiç kullanılmamış!");
            suggestions.Add("📍 Konumunu gözden geçirin");
            suggestions.Add("🎯 Özel promosyon düşünün");
            suggestions.Add("🔄 Başka amaçla kullanılabilir");
            return suggestions;
        }

        // Kullanılan masalar için öneriler
        if (revenue < 50)
        {
            suggestions.Add("💡 Düşük ciro - Marketing gerekli");
            suggestions.Add("📱 QR menü tanıtımı yapın");
        }
        else if (revenue > 500)
        {
            suggestions.Add("⭐ Mükemmel performans!");
            suggestions.Add("🏆 Bu masa örnek alınabilir");
        }

        if (orderCount < 3)
        {
            suggestions.Add("📈 Sipariş sayısı artırılabilir");
            suggestions.Add("🍽️ Combo menü önerileri");
        }

        if (sessionDuration < 30)
        {
            suggestions.Add("⏱️ Oturma süresi kısa");
            suggestions.Add("☕ İkram servisi düşünün");
        }
        else if (sessionDuration > 120)
        {
            suggestions.Add("🎯 Uzun oturma - Ek sipariş fırsatı");
            suggestions.Add("🍰 Tatlı önerileri sunun");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("✅ Performans dengeli");
            suggestions.Add("📊 Mevcut stratejiyi sürdürün");
        }

        return suggestions;
    }
    #endregion


    // ReportController.cs içine ekleyin

    [HttpGet]
    public async Task<IActionResult> GetStaffDetailApi(Guid personId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Personel detaylı analizi
            var staffDetail = await _dbContext.AddtionHistories
                .Where(h => h.PersonId == personId && h.CreatedAt >= startDate && h.CreatedAt <= endDate)
                .GroupBy(h => 1)
                .Select(g => new
                {
                    TotalSales = g.Sum(h => h.TotalPrice),
                    TotalOrders = g.Count(),
                    AverageOrderValue = g.Average(h => h.TotalPrice),
                    UniqueCustomers = g.Select(h => h.AddionStatusId).Distinct().Count(),
                    TopProducts = g.GroupBy(h => h.ProductName)
                                  .Select(pg => new
                                  {
                                      ProductName = pg.Key,
                                      Quantity = pg.Sum(h => h.ProductQuantity),
                                      Revenue = pg.Sum(h => h.TotalPrice)
                                  })
                                  .OrderByDescending(p => p.Revenue)
                                  .Take(5)
                                  .ToList(),
                    HourlyDistribution = g.GroupBy(h => h.CreatedAt.Hour)
                                        .Select(hg => new
                                        {
                                            Hour = hg.Key,
                                            Orders = hg.Count(),
                                            Sales = hg.Sum(h => h.TotalPrice)
                                        })
                                        .OrderBy(h => h.Hour)
                                        .ToList(),
                    DailyPerformance = g.GroupBy(h => h.CreatedAt.Date)
                                       .Select(dg => new
                                       {
                                           Date = dg.Key,
                                           Orders = dg.Count(),
                                           Sales = dg.Sum(h => h.TotalPrice)
                                       })
                                       .OrderBy(d => d.Date)
                                       .ToList()
                })
                .FirstOrDefaultAsync();

            return Json(new { success = true, data = staffDetail });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Hızlı istatistikler için
    // Controllers/ReportController.cs - Bu metodları da ekleyin

    [HttpGet]
    public async Task<IActionResult> GetQuickStatsApi()
    {
        try
        {
            var currentMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            var today = DateTime.Today;

            var stats = new
            {
                monthlyRevenue = await _dbContext.AddtionHistories
                    .Where(h => h.CreatedAt >= currentMonth)
                    .SumAsync(h => h.TotalPrice),

                activeStaff = await _dbContext.Persons
                    .CountAsync(p => p.IsActive && !p.IsDeleted && p.AccessType == AccessType.Person),

                topProduct = await _dbContext.AddtionHistories
                    .Where(h => h.CreatedAt >= currentMonth)
                    .GroupBy(h => h.ProductName)
                    .OrderByDescending(g => g.Sum(h => h.TotalPrice))
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync(),

                occupancyRate = await CalculateOccupancyRate()
            };

            return Json(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<double> CalculateOccupancyRate()
    {
        var totalTables = await _dbContext.Tables.CountAsync(t => t.IsActive);
        var occupiedTables = await _dbContext.Tables.CountAsync(t => t.IsActive && t.AddionStatus.HasValue);

        return totalTables > 0 ? Math.Round((double)occupiedTables / totalTables * 100, 1) : 0;
    }
}