namespace KufeArtFullAdission.Entity;

public sealed class CampaignDbEntity : BaseDbEntity
{
    public string Name { get; set; } // "Kahve Kampanyası"
    public string Description { get; set; } // "10 kahve al 1 bedava"
    public Guid ProductId { get; set; } // Hangi ürün
    public int RequiredQuantity { get; set; } // 10 (dinamik)
    public int FreeQuantity { get; set; } // 1
    public int ValidityDays { get; set; } = 30; // Sabit 1 ay
    public bool IsActive { get; set; } = true;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; } // İsteğe bağlı bitiş
}
