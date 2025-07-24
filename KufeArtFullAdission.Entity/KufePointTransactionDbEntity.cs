using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class KufePointTransactionDbEntity : BaseDbEntity
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; } // Hangi ürün için
    public PointType Type { get; set; } // Kazanıldı/Harcandı
    public int Points { get; set; } // +100 veya -5000
    public string Description { get; set; } // "Kahve alışverişi" 
}
