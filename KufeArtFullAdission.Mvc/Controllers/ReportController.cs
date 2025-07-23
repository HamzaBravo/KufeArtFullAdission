using AppDbContext;
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
            var data = await GetStaffPerformanceData(startDate, endDate);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
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

    [HttpGet]
    public async Task<IActionResult> GetTableHeatmapApi(DateTime startDate, DateTime endDate)
    {
        try
        {
            var data = await GetTableHeatmapData(startDate, endDate);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Private Methods

    // Controllers/ReportController.cs - Düzeltilmiş versiyonu

    private async Task<object> GetStaffPerformanceData(DateTime startDate, DateTime endDate)
    {

        startDate= startDate.Date.AddSeconds(-1); // Başlangıç tarihini bir gün öncesine al
        endDate = endDate.Date.AddDays(1).AddSeconds(-1); // Bitiş tarihini bir gün sonrasına al


        // 🔥 HATAYI ÇÖZEN VERSİYON - Önce verileri çekip sonra grupla
        var orderData = await _dbContext.AddtionHistories
            .Where(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate)
            .Select(h => new
            {
                h.PersonId,
                h.PersonFullName,
                h.TotalPrice,
                h.ProductName,
                h.ProductQuantity,
                h.AddionStatusId,
                h.CreatedAt,
                Date = h.CreatedAt.Date // SQL'de Date hesaplama
            })
            .ToListAsync(); // 🎯 Bu noktada SQL sorgusu tamamlanıyor

        // 🔥 Artık memory'de LINQ ile güvenle grupla
        var staffOrders = orderData
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
                                    .FirstOrDefault(),
                UniqueCustomers = g.Select(h => h.AddionStatusId).Distinct().Count(),
                OrdersByDay = g.GroupBy(h => h.Date)
                              .ToDictionary(dg => dg.Key, dg => dg.Count())
            })
            .OrderByDescending(s => s.TotalSales)
            .ToList();

        // Günlük performans trendi
        var dailyPerformance = orderData
            .GroupBy(h => new { h.Date, h.PersonId, h.PersonFullName })
            .Select(g => new
            {
                Date = g.Key.Date.ToString("yyyy-MM-dd"), // JSON serialize için string
                PersonId = g.Key.PersonId,
                PersonName = g.Key.PersonFullName,
                DailySales = g.Sum(h => h.TotalPrice),
                DailyOrders = g.Count()
            })
            .ToList();

        // En çok satan ürünler (personel bazlı)
        var topProductsByStaff = orderData
            .GroupBy(h => new { h.PersonId, h.PersonFullName, h.ProductName })
            .Select(g => new
            {
                PersonId = g.Key.PersonId,
                PersonName = g.Key.PersonFullName,
                ProductName = g.Key.ProductName,
                Quantity = g.Sum(h => h.ProductQuantity),
                Revenue = g.Sum(h => h.TotalPrice)
            })
            .ToList();

        return new
        {
            StaffSummary = staffOrders,
            DailyTrends = dailyPerformance,
            TopProducts = topProductsByStaff,
            PeriodStart = startDate.ToString("yyyy-MM-dd"),
            PeriodEnd = endDate.ToString("yyyy-MM-dd"),
            TotalStaff = staffOrders.Count,
            TotalRevenue = staffOrders.Sum(s => s.TotalSales),
            AvgOrdersPerStaff = staffOrders.Any() ? staffOrders.Average(s => s.TotalOrders) : 0
        };
    }

    // Diğer metodlar için de aynı tekniği uygulayalım
    private async Task<object> GetSalesAnalysisData(DateTime startDate, DateTime endDate)
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
                h.CreatedAt,
                Date = h.CreatedAt.Date,
                Hour = h.CreatedAt.Hour
            })
            .ToListAsync();

        // Products tablosundan kategori bilgilerini al
        var productCategories = await _dbContext.Products
            .Select(p => new { p.Name, p.CategoryName })
            .ToListAsync();

        // 🔥 Memory'de grupla
        var dailySales = orderData
            .GroupBy(h => h.Date)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                TotalSales = g.Sum(h => h.TotalPrice),
                OrderCount = g.Count(),
                AvgOrderValue = g.Average(h => h.TotalPrice),
                UniqueCustomers = g.Select(h => h.AddionStatusId).Distinct().Count()
            })
            .OrderBy(d => d.Date)
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
                Category = g.Key ?? "Diğer",
                TotalSales = g.Sum(x => x.Order.TotalPrice),
                Quantity = g.Sum(x => x.Order.ProductQuantity),
                OrderCount = g.Count(),
                AvgPrice = g.Average(x => x.Order.ProductPrice)
            })
            .OrderByDescending(c => c.TotalSales)
            .ToList();

        // En çok Satan ürünler
        var topProducts = orderData
            .GroupBy(h => h.ProductName)
            .Select(g => new
            {
                ProductName = g.Key,
                TotalSales = g.Sum(h => h.TotalPrice),
                Quantity = g.Sum(h => h.ProductQuantity),
                OrderCount = g.Count(),
                AvgPrice = g.Average(h => h.ProductPrice)
            })
            .OrderByDescending(p => p.TotalSales)
            .Take(10)
            .ToList();

        // Saatlik satış
        var hourlySales = orderData
            .GroupBy(h => h.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                TotalSales = g.Sum(h => h.TotalPrice),
                OrderCount = g.Count()
            })
            .OrderBy(h => h.Hour)
            .ToList();

        return new
        {
            DailySales = dailySales,
            CategoryBreakdown = categoryBreakdown,
            TopProducts = topProducts,
            HourlySales = hourlySales,
            Summary = new
            {
                TotalRevenue = dailySales.Sum(d => d.TotalSales),
                TotalOrders = dailySales.Sum(d => d.OrderCount),
                AvgDailyRevenue = dailySales.Any() ? dailySales.Average(d => d.TotalSales) : 0,
                BestDay = dailySales.OrderByDescending(d => d.TotalSales).FirstOrDefault(),
                PeakHour = hourlySales.OrderByDescending(h => h.TotalSales).FirstOrDefault()
            }
        };
    }

    private async Task<object> GetTableHeatmapData(DateTime startDate, DateTime endDate)
    {
        // 🔥 Raw data çek
        var orderData = await _dbContext.AddtionHistories
            .Where(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate)
            .Select(h => new
            {
                h.AddionStatusId,
                h.TotalPrice,
                h.CreatedAt,
                Date = h.CreatedAt.Date,
                Hour = h.CreatedAt.Hour
            })
            .ToListAsync();

        // Table bilgilerini al
        var tableData = await _dbContext.Tables
            .Where(t => t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Category,
                t.AddionStatus
            })
            .ToListAsync();

        // 🔥 Memory'de JOIN ve GROUP BY yap
        var tableStats = orderData
            .Join(tableData,
                  o => o.AddionStatusId,
                  t => t.AddionStatus,
                  (o, t) => new { Order = o, Table = t })
            .GroupBy(x => new { x.Table.Id, x.Table.Name, x.Table.Category })
            .Select(g => new
            {
                TableId = g.Key.Id,
                TableName = g.Key.Name,
                Category = g.Key.Category,
                TotalRevenue = g.Sum(x => x.Order.TotalPrice),
                OrderCount = g.Count(),
                UniqueCustomers = g.Select(x => x.Order.AddionStatusId).Distinct().Count(),
                AvgOrderValue = g.Average(x => x.Order.TotalPrice),
                TotalCustomerTime = g.Select(x => x.Order.AddionStatusId).Distinct().Count()
            })
            .OrderByDescending(t => t.TotalRevenue)
            .ToList();

        // Kategori performansı
        var categoryPerformance = tableStats
            .GroupBy(t => t.Category)
            .Select(g => new
            {
                Category = g.Key,
                TableCount = g.Count(),
                TotalRevenue = g.Sum(t => t.TotalRevenue),
                AvgRevenuePerTable = g.Average(t => t.TotalRevenue),
                TotalOrders = g.Sum(t => t.OrderCount)
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        // Günlük doluluk
        var dailyOccupancy = orderData
            .GroupBy(o => o.Date)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                ActiveTables = g.Select(o => o.AddionStatusId).Distinct().Count(),
                TotalOrders = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Saatlik analiz
        var timeSlotAnalysis = orderData
            .GroupBy(o => o.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                AvgActiveTables = g.Select(o => o.AddionStatusId).Distinct().Count(),
                TotalActivity = g.Count()
            })
            .OrderBy(h => h.Hour)
            .ToList();

        return new
        {
            TableStats = tableStats,
            CategoryPerformance = categoryPerformance,
            DailyOccupancy = dailyOccupancy,
            TimeSlotAnalysis = timeSlotAnalysis,
            Summary = new
            {
                TotalTables = tableStats.Count,
                MostProfitableTable = tableStats.FirstOrDefault(),
                AvgRevenuePerTable = tableStats.Any() ? tableStats.Average(t => t.TotalRevenue) : 0,
                TotalCategories = categoryPerformance.Count
            }
        };
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
    [HttpGet]
    public async Task<IActionResult> GetQuickStatsApi()
    {
        try
        {
            var currentMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            var today = DateTime.Today;

            var stats = new
            {
                MonthlyRevenue = await _dbContext.AddtionHistories
                    .Where(h => h.CreatedAt >= currentMonth)
                    .SumAsync(h => h.TotalPrice),

                ActiveStaff = await _dbContext.Persons
                    .CountAsync(p => p.IsActive && !p.IsDeleted && p.AccessType == AccessType.Person),

                TopProduct = await _dbContext.AddtionHistories
                    .Where(h => h.CreatedAt >= currentMonth)
                    .GroupBy(h => h.ProductName)
                    .OrderByDescending(g => g.Sum(h => h.TotalPrice))
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync(),

                OccupancyRate = await CalculateOccupancyRate()
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