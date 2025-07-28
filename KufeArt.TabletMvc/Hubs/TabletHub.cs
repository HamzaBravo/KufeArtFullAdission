using Microsoft.AspNetCore.SignalR;

namespace KufeArt.TabletMvc.Hubs
{
    public class TabletHub : Hub
    {
        public async Task JoinKitchenGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Kitchen");
            await Clients.Caller.SendAsync("JoinedKitchenGroup", "Kitchen grubuna katıldı");
        }

        public async Task JoinBarGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Bar");
            await Clients.Caller.SendAsync("JoinedBarGroup", "Bar grubuna katıldı");
        }
    }
}