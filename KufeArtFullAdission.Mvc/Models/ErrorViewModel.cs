using System;
using System.Collections.Generic;

namespace KufeArtFullAdission.Mvc.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class TableDetailViewModel
    {
        public TableInfo Table { get; set; }
        public List<OrderInfo> Orders { get; set; } = new List<OrderInfo>();
        public double TotalAmount { get; set; }
    }

    public class TableInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Guid? AddionStatus { get; set; }
        public bool IsOccupied { get; set; }
    }

    public class OrderInfo
    {
        public Guid Id { get; set; }
        public string ShorLabel { get; set; }
        public string ProductName { get; set; }
        public double ProductPrice { get; set; }
        public int ProductQuantity { get; set; }
        public double TotalPrice { get; set; }
        public string PersonFullName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
