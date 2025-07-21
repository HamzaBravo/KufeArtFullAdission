using AppDbContext;
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
            // Dashboard kartlarý için veriler
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

                // Kategori bazlý grupla
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
                    return Json(new { success = false, message = "Masa bulunamadý!" });

                var result = new TableDetailViewModel
                {
                    Table = new TableInfo
                    {
                        Id = table.Id,
                        Name = table.Name,
                        Category = table.Category,
                        AddionStatus = table.AddionStatus,
                        IsOccupied = table.AddionStatus.HasValue
                    }
                };

                // Eðer masa dolu ise sipariþleri getir
                if (table.AddionStatus.HasValue)
                {
                    result.Orders = await _dbContext.AddtionHistories
                        .Where(x => x.AddionStatusId == table.AddionStatus)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => new OrderInfo
                        {
                            Id = x.Id,
                            ShorLabel = x.ShorLabel,
                            ProductName = x.ProductName,
                            ProductPrice = x.ProductPrice,
                            ProductQuantity = x.ProductQuantity,
                            TotalPrice = x.TotalPrice,
                            PersonFullName = x.PersonFullName,
                            CreatedAt = x.CreatedAt
                        })
                        .ToListAsync();

                    result.TotalAmount = result.Orders.Sum(x => x.TotalPrice);
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> OpenTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamadý!" });

                if (table.AddionStatus.HasValue)
                    return Json(new { success = false, message = "Bu masada zaten açýk hesap var!" });

                // Yeni hesap ID'si oluþtur
                var newAccountId = Guid.NewGuid();

                // Masaya hesap ID'si ata
                table.AddionStatus = newAccountId;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap baþarýyla açýldý!",
                    accountId = newAccountId,
                    tableId = tableId
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluþtu: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CloseTableAccount(Guid tableId)
        {
            try
            {
                var table = await _dbContext.Tables.FindAsync(tableId);
                if (table == null)
                    return Json(new { success = false, message = "Masa bulunamadý!" });

                if (!table.AddionStatus.HasValue)
                    return Json(new { success = false, message = "Bu masada açýk hesap yok!" });

                // Hesap kapatma iþlemi
                table.AddionStatus = null;

                await _dbContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Hesap baþarýyla kapatýldý!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluþtu: " + ex.Message });
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

            // 2. Her masa için ayrý ayrý hesapla (Memory'de)
            foreach (var table in tables)
            {
                double totalAmount = 0;
                DateTime? openedAt = null;

                if (table.AddionStatus.HasValue)
                {
                    // Sipariþleri getir
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
                        // Sipariþi yoksa masa açýlýþ zamaný olarak þimdiyi kullan
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