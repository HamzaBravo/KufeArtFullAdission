namespace KufeArtFullAdission.Entity;

public sealed class PaymentItemDbEntity : BaseDbEntity
{
    public Guid PaymentId { get; set; } // PaymentDbEntity referansı
    public Guid OrderItemId { get; set; } // AddtionHistoryDbEntity referansı
    public double PaidAmount { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}
