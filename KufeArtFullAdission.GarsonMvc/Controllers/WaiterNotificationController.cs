// KufeArtFullAdission.GarsonMvc/Controllers/WaiterNotificationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KufeArtFullAdission.GarsonMvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WaiterNotificationController(IHubContext<WaiterHub> _hubContext) : ControllerBase
{
    // KufeArtFullAdission.GarsonMvc/Controllers/WaiterNotificationController.cs
    [HttpPost]
    public async Task<IActionResult> ReceiveNotification([FromBody] WaiterNotificationDto notification)
    {
        try
        {
            // 🔥 Eski haline döndür - mevcut sistemi bozmayalım
            await _hubContext.Clients.Group("AllWaiters").SendAsync("AdminNotification", new
            {
                Type = notification.Type,
                TableId = notification.TableId,
                TableName = notification.TableName,
                Message = notification.Message,
                Timestamp = DateTime.Now
            });

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    // ✅ YENİ: Tablet'den sipariş tamamlama bildirimi
    [HttpPost("order-completed")]
    public async Task<IActionResult> OrderCompletedNotification([FromBody] OrderCompletedNotificationDto notification)
    {
        try
        {
            Console.WriteLine($"📱➡️🧑‍💼 Tablet'den garson bildirimi: {notification.TableName} - {notification.Department}");

            // Garsonlara sipariş hazır bildirimi gönder
            await _hubContext.Clients.Group("AllWaiters").SendAsync("OrderCompletedFromTablet", new
            {
                Type = "OrderCompleted",
                OrderBatchId = notification.OrderBatchId,
                TableId = notification.TableId,
                TableName = notification.TableName,
                Department = notification.Department,
                CompletedBy = notification.CompletedBy,
                Message = $"🍽️ {notification.TableName} siparişi hazır! ({notification.Department})",
                Icon = notification.Department == "Kitchen" ? "fas fa-utensils" : "fas fa-cocktail",
                Color = "success",
                Timestamp = DateTime.Now,
                Priority = "high"
            });

            return Ok(new { success = true, message = "Garsonlara bildirim gönderildi" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Garson bildirimi hatası: {ex.Message}");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class WaiterNotificationDto
{
    public string Type { get; set; } = "";
    public Guid? TableId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = "";
}

public class OrderCompletedNotificationDto
{
    public string OrderBatchId { get; set; } = "";
    public Guid TableId { get; set; }
    public string TableName { get; set; } = "";
    public string Department { get; set; } = "";
    public string CompletedBy { get; set; } = "";
}