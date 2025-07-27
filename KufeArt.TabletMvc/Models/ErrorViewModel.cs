using KufeArtFullAdission.Enums;
using System.ComponentModel.DataAnnotations;

namespace KufeArt.TabletMvc.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class TabletLoginModel
    {
        [Required(ErrorMessage = "Lütfen bir bölüm seçin")]
        public string Department { get; set; } = ""; // "Kitchen" veya "Bar"

        [Required(ErrorMessage = "Tablet adý gerekli")]
        [StringLength(50, ErrorMessage = "Tablet adý en fazla 50 karakter olmalý")]
        public string TabletName { get; set; } = "";

        public string? Note { get; set; } // Opsiyonel not
    }

    public class TabletSessionModel
    {
        public string Department { get; set; } = "";
        public string TabletName { get; set; } = "";
        public DateTime LoginTime { get; set; }
        public string? Note { get; set; }
    }


    public class OrderDashboardModel
    {
        public List<TabletOrderModel> Orders { get; set; } = new();
        public TabletSessionModel Session { get; set; } = new();
        public DashboardStatsModel Stats { get; set; } = new();
    }

    public class TabletOrderModel
    {
        public Guid OrderBatchId { get; set; }
        public Guid TableId { get; set; }
        public string TableName { get; set; } = "";
        public string WaiterName { get; set; } = "";
        public DateTime OrderTime { get; set; }
        public List<TabletOrderItemModel> Items { get; set; } = new();
        public double TotalAmount { get; set; }
        public string Status { get; set; } = "New"; // "New", "InProgress", "Ready"
        public string? Note { get; set; }
        public bool IsNew { get; set; } = true; // Yeni sipariþ mi?
    }

    public class TabletOrderItemModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public double Price { get; set; }
        public ProductOrderType ProductType { get; set; }
        public string CategoryName { get; set; } = "";
    }

    public class DashboardStatsModel
    {
        public int TotalOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public double TotalAmountToday { get; set; }
    }
}
