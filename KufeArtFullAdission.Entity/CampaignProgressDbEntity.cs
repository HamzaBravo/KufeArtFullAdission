namespace KufeArtFullAdission.Entity;

public sealed class CampaignProgressDbEntity : BaseDbEntity
{
    public Guid CustomerId { get; set; }
    public Guid CampaignId { get; set; }
    public int CurrentQuantity { get; set; } // Şu ana kadar aldığı
    public int EarnedFreeQuantity { get; set; } // Kazandığı bedava adet
    public DateTime LastPurchaseDate { get; set; }
    public DateTime CampaignStartDate { get; set; } // 1 aylık süre kontrolü için
}
