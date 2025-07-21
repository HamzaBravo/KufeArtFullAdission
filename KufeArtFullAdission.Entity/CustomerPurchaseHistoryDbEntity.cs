namespace KufeArtFullAdission.Entity;

public sealed class CustomerPurchaseHistoryDbEntity : BaseDbEntity
{
    public Guid CustomerId { get; set; } // CustomerDbEntity referansı
    public Guid ProductId { get; set; }
    public Guid? CampaignId { get; set; } // Hangi kampanya kapsamında
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }
    public bool IsFreeCampaignProduct { get; set; } // Hediye ürün mü?
}
