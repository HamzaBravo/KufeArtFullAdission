using System;
using System.Collections.Generic;

namespace KufeArt.PrinterService.Models;

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

public class OrderPrintData
{
    public string OrderId { get; set; } = "";
    public string TableName { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public DateTime OrderTime { get; set; } = DateTime.Now;
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    public string Notes { get; set; } = "";
}

public class OrderItem
{
    public string ProductName { get; set; } = "";
    public string ProductType { get; set; } = ""; // "Kitchen" veya "Bar" 
    public int Quantity { get; set; } = 1;
    public decimal Price { get; set; } = 0;
    public string Notes { get; set; } = "";
}

public class ServiceStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastHeartbeat { get; set; } = DateTime.Now;
    public int TotalPrinters { get; set; }
    public int KitchenPrinters { get; set; }
    public int BarPrinters { get; set; }
    public List<string> ActivePrinters { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
}