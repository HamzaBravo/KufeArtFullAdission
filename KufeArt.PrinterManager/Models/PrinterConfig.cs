namespace KufeArt.PrinterManager.Models;

public class PrinterConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Atanmamış"; // "Mutfak", "Bar", "Atanmamış"
    public bool IsEnabled { get; set; } = true;
    public DateTime LastModified { get; set; } = DateTime.Now;
}

public class PrinterManagerConfig
{
    public List<PrinterConfig> Printers { get; set; } = new List<PrinterConfig>();
    public DateTime LastUpdate { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0";
    public string ConfigPath { get; set; } = "";
}

// SignalR'dan gelen sipariş bildirimi
public class OrderNotificationModel
{
    public string Type { get; set; } = "";
    public Guid TableId { get; set; }
    public string TableName { get; set; } = "";
    public double TotalAmount { get; set; }
    public string WaiterName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Message { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";

    // Ürün detayları
    public List<OrderItemModel>? Items { get; set; } = new();
}

// Yazdırma için detaylı sipariş modeli
public class DetailedOrderModel
{
    public Guid TableId { get; set; }
    public string TableName { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public DateTime OrderTime { get; set; }
    public List<OrderItemModel> Items { get; set; } = new();
    public double TotalAmount { get; set; }
    public string? Note { get; set; }
}

// Sipariş kalemi
public class OrderItemModel
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public double Price { get; set; }
    public double TotalPrice { get; set; }
    public string ProductType { get; set; } = ""; // "Mutfak" veya "Bar"
    public string CategoryName { get; set; } = "";
}

// Log kayıtları için
public class PrintLogModel
{
    public DateTime PrintTime { get; set; } = DateTime.Now;
    public string TableName { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public string PrinterType { get; set; } = "";
    public string Status { get; set; } = ""; // "Success", "Failed"
    public string? ErrorMessage { get; set; }
    public int ItemCount { get; set; }
}