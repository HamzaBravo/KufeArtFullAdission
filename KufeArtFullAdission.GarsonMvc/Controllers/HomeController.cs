// KufeArtFullAdission.GarsonMvc/Controllers/HomeController.cs
using AppDbContext;
using KufeArtFullAdission.GarsonMvc.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                // Masa sipariþ kontrolü
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