using KufeArtFullAdission.Enums;
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
        public Guid OrderBatchId { get; set; }
    }

    public class OrderSubmissionDto
    {
        public Guid TableId { get; set; }
        public string WaiterNote { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
    }

    // KufeArtFullAdission.Mvc/Models/ErrorViewModel.cs - Bu dosyanýn sonuna ekle

    public class PaymentDto
    {
        public Guid TableId { get; set; }
        public Guid AddionStatusId { get; set; }
        public PaymentType PaymentType { get; set; }
        public double Amount { get; set; }
        public string ShortLabel { get; set; }
        public Guid PersonId { get; set; }
    }

    public class PaymentInfo
    {
        public Guid Id { get; set; }
        public PaymentType PaymentType { get; set; }
        public double Amount { get; set; }
        public string ShortLabel { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PersonFullName { get; set; }
    }

    public class QuickPaymentDto
    {
        public Guid TableId { get; set; }
        public string PaymentMode { get; set; } // "full", "half", "tip15", "label", "custom"
        public PaymentType PaymentType { get; set; } // Cash, Card
        public string PaymentLabel { get; set; } // Etiket ödemesi için
        public double CustomAmount { get; set; } // Özel tutar için
    }
}
