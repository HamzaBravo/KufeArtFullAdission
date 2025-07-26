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
}

public class WaiterNotificationDto
{
    public string Type { get; set; } = "";
    public Guid? TableId { get; set; }
    public string? TableName { get; set; }
    public string Message { get; set; } = "";
}