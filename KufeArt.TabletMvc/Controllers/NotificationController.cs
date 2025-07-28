using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using KufeArt.TabletMvc.Hubs;

namespace KufeArt.TabletMvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly IHubContext<TabletHub> _hubContext;

        public NotificationController(IHubContext<TabletHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("tablet-kitchen")]
        public async Task<IActionResult> NotifyKitchen([FromBody] TabletNotificationDto request)
        {
            try
            {
                await _hubContext.Clients.Group("Kitchen").SendAsync("NewOrderReceived", request.OrderData);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("tablet-bar")]
        public async Task<IActionResult> NotifyBar([FromBody] TabletNotificationDto request)
        {
            try
            {
                await _hubContext.Clients.Group("Bar").SendAsync("NewOrderReceived", request.OrderData);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class TabletNotificationDto
    {
        public object OrderData { get; set; }
        public string Department { get; set; }
    }
}