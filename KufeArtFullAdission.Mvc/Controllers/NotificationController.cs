// KufeArtFullAdission.Mvc/Controllers/NotificationController.cs
using KufeArtFullAdission.Mvc.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace KufeArtFullAdission.Mvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController(IHubContext<OrderHub> _hubContext) : ControllerBase
{
    [HttpPost("new-order")]
    [AllowAnonymous] // Garson panelinden gelecek
    public async Task<IActionResult> NewOrder([FromBody] NewOrderNotificationDto notification)
    {
        try
        {
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
                Color = "success"
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
    [Authorize]
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
    [Authorize]
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

    // KufeArtFullAdission.Mvc/Controllers/NotificationController.cs - Yeni method ekle

    [HttpPost("notify-waiters")]
    [Authorize]
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