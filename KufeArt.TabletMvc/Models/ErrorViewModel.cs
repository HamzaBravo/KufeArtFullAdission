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



    public class TabletOrderModel
    {
        public string OrderBatchId { get; set; } = "";
        public string TableId { get; set; } = "";
        public string TableName { get; set; } = "";
        public string WaiterName { get; set; } = "";
        public DateTime OrderTime { get; set; }
        public string Status { get; set; } = "New"; // New, InProgress, Ready
        public double TotalAmount { get; set; }
        public List<TabletOrderItemModel> Items { get; set; } = new();
        public bool IsNew { get; set; }
    }

    public class TabletOrderItemModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string ProductType { get; set; } = "";
        public string CategoryName { get; set; } = "";
    }

    public class UpdateOrderStatusModel
    {
        public string Status { get; set; } = "";
    }

    public class DashboardStatsModel
    {
        public int PendingCount { get; set; }
        public int TodayCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class OrderDashboardModel
    {
        public TabletSessionModel Session { get; set; } = new();
        public List<TabletOrderModel> Orders { get; set; } = new();
        public DashboardStatsModel Stats { get; set; } = new();
    }
}
