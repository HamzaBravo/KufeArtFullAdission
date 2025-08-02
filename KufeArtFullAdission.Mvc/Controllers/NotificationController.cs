// KufeArtFullAdission.Mvc/Controllers/NotificationController.cs
using AppDbContext;
using KufeArtFullAdission.Enums;
using KufeArtFullAdission.Mvc.Hubs;
using KufeArtFullAdission.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController(IHubContext<OrderHub> _hubContext,DBContext _dBContext) : ControllerBase
{

    [HttpPost("new-order")]
    [AllowAnonymous] // Garson panelinden gelecek
    public async Task<IActionResult> NewOrder([FromBody] NewOrderNotificationDto notification)
    {
        try
        {
            // ✅ Sipariş detaylarını veritabanından çek
            var orderItems = await GetOrderItemsAsync(notification.TableId);

            var orderData = new
            {
                Type = "NewOrder",
                TableId = notification.TableId,
                TableName = notification.TableName,
                TotalAmount = notification.TotalAmount,
                WaiterName = notification.WaiterName,
                Timestamp = notification.Timestamp,
                Message = $"{notification.WaiterName} - {notification.TableName} için yeni sipariş: {notification.TotalAmount:C2}",
                Icon = "fas fa-shopping-cart",
                Color = "success",
                // ✅ YENİ: Ürün detayları eklendi
                Items = orderItems
            };

            // Admin paneline bildirim gönder
            await _hubContext.Clients.Group("AdminPanel").SendAsync("NewOrderReceived", orderData);

            return Ok(new { success = true, message = "Bildirim gönderildi" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("table-status-change")]
    public async Task<IActionResult> TableStatusChange([FromBody] TableStatusChangeDto statusChange)
    {
        try
        {
            var tableData = new
            {
                Type = "TableStatusChange",
                TableId = statusChange.TableId,
                TableName = statusChange.TableName,
                Status = statusChange.Status,
                Timestamp = DateTime.Now
            };

            // Tüm bağlı kullanıcılara bildirim
            await _hubContext.Clients.All.SendAsync("TableStatusChanged", tableData);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("order-complete")]

    public async Task<IActionResult> OrderComplete([FromBody] OrderCompleteDto orderComplete)
    {
        try
        {
            var orderData = new
            {
                Type = "OrderComplete",
                TableId = orderComplete.TableId,
                TableName = orderComplete.TableName,
                Message = $"{orderComplete.TableName} siparişi hazır!",
                Timestamp = DateTime.Now,
                Icon = "fas fa-check-circle",
                Color = "info"
            };

            // Garsonlara bildirim
            await _hubContext.Clients.Group("Waiters").SendAsync("OrderCompleted", orderData);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    [HttpPost("notify-waiters")]
    public async Task<IActionResult> NotifyWaiters([FromBody] WaiterNotificationDto notification)
    {
        try
        {
            // Garson paneline HTTP request gönder
            var httpClient = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>()
                .CreateClient("WaiterPanel");

            var response = await httpClient.PostAsJsonAsync("/api/waiter-notification", notification);

            if (response.IsSuccessStatusCode)
            {
                return Ok(new { success = true, message = "Garsonlara bildirim gönderildi" });
            }

            return BadRequest(new { success = false, message = "Garson bildirimi başarısız" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    [HttpPost("table-operation")]
    [AllowAnonymous] // Garson panelinden gelecek
    public async Task<IActionResult> TableOperation([FromBody] TableOperationNotificationDto notification)
    {
        try
        {
            Console.WriteLine($"📨 Admin panele masa işlemi bildirimi geldi: {notification.Action}");

            var operationData = new
            {
                type = "TableOperation",
                action = notification.Action,
                sourceTableId = notification.SourceTableId,
                sourceTableName = notification.SourceTableName,
                targetTableId = notification.TargetTableId,
                targetTableName = notification.TargetTableName,
                waiterName = notification.WaiterName,
                message = notification.Message,
                timestamp = notification.Timestamp,
                icon = notification.Icon,
                color = notification.Color
            };

            // Admin paneline bildirim gönder
            await _hubContext.Clients.Group("AdminPanel").SendAsync("TableOperationReceived", operationData);

            Console.WriteLine($"✅ Admin panele masa işlemi bildirimi gönderildi: {notification.Message}");
            return Ok(new { success = true, message = "Masa işlemi bildirimi gönderildi" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Masa işlemi bildirimi hatası: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("order-item-cancelled")]
    [AllowAnonymous] // Garson panelinden gelecek
    public async Task<IActionResult> OrderItemCancelled([FromBody] OrderItemCancelledNotificationDto notification)
    {
        try
        {
            Console.WriteLine($"📨 Admin panele sipariş iptal bildirimi geldi: {notification.ProductName}");

            var cancelData = new
            {
                Type = "OrderItemCancelled",
                TableName = notification.TableName,
                ProductName = notification.ProductName,
                WaiterName = notification.WaiterName,
                Message = notification.Message,
                Timestamp = notification.Timestamp,
                Icon = notification.Icon,
                Color = notification.Color
            };

            // Admin paneline bildirim gönder
            await _hubContext.Clients.Group("AdminPanel").SendAsync("OrderItemCancelledReceived", cancelData);

            Console.WriteLine($"✅ Admin panele sipariş iptal bildirimi gönderildi: {notification.Message}");
            return Ok(new { success = true, message = "Sipariş iptal bildirimi gönderildi" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sipariş iptal bildirimi hatası: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("order-cancelled")]
    [AllowAnonymous] // Garson panelinden gelecek
    public async Task<IActionResult> OrderCancelled([FromBody] OrderCancelledNotificationDto notification)
    {
        try
        {
            Console.WriteLine($"📨 Admin panele sipariş iptal bildirimi geldi: {notification.ProductName}");

            var cancelData = new
            {
                Type = notification.Type,
                TableName = notification.TableName,
                ProductName = notification.ProductName,
                WaiterName = notification.WaiterName,
                Message = notification.Message,
                Timestamp = notification.Timestamp,
                Icon = notification.Icon,
                Color = notification.Color
            };

            // Admin paneline bildirim gönder
            await _hubContext.Clients.Group("AdminPanel").SendAsync("OrderCancelledReceived", cancelData);

            Console.WriteLine($"✅ Admin panele sipariş iptal bildirimi gönderildi: {notification.Message}");
            return Ok(new { success = true, message = "Sipariş iptal bildirimi gönderildi" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Sipariş iptal bildirimi hatası: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private async Task<List<object>> GetOrderItemsAsync(Guid tableId)
    {
        try
        {
            var table = await _dBContext.Tables.FindAsync(tableId);
            if (table?.AddionStatus == null)
                return new List<object>();

            // En son batch'i al (son sipariş)
            var latestBatch = await _dBContext.AddtionHistories
                .Where(h => h.AddionStatusId == table.AddionStatus)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => h.OrderBatchId)
                .FirstOrDefaultAsync();

            if (latestBatch == Guid.Empty)
                return new List<object>();

            // Batch'e ait ürünleri al ve ProductDbEntity ile join et
            var orderItems = await (from history in _dBContext.AddtionHistories
                                    join product in _dBContext.Products on history.ProductName equals product.Name
                                    where history.OrderBatchId == latestBatch
                                    select new
                                    {
                                        ProductName = history.ProductName,
                                        Quantity = history.ProductQuantity,
                                        Price = history.ProductPrice,
                                        TotalPrice = history.TotalPrice,
                                        CategoryName = product.CategoryName,
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
}

public class OrderCancelledNotificationDto
{
    public string Type { get; set; } = "";
    public string TableName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}

public class TableOperationNotificationDto
{
    public string Action { get; set; } = "";
    public Guid SourceTableId { get; set; }
    public string SourceTableName { get; set; } = "";
    public Guid? TargetTableId { get; set; }
    public string? TargetTableName { get; set; }
    public string WaiterName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}

public class OrderItemCancelledNotificationDto
{
    public string TableName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}

public class WaiterNotificationDto
{
    public string Type { get; set; } = "TableUpdate";
    public Guid? TableId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = "";
}

// DTO Classes
public class NewOrderNotificationDto
{
    public Guid TableId { get; set; }
    public string TableName { get; set; }
    public double TotalAmount { get; set; }
    public string WaiterName { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TableStatusChangeDto
{
    public Guid TableId { get; set; }
    public string TableName { get; set; }
    public string Status { get; set; }
}

public class OrderCompleteDto
{
    public Guid TableId { get; set; }
    public string TableName { get; set; }
}