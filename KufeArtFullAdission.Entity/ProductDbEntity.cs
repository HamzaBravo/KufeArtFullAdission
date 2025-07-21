using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class ProductDbEntity:BaseDbEntity
{
    public string Name { get; set; } // ürün adı
    public string Description { get; set; } // ürün açıklaması opsiyonel
    public double Price { get; set; } // ürün birim fiyatı
    public bool IsQrMenu { get; set; } // ürün qr menüde gösterilsin mi
    public bool IsTvMenu { get; set; } // ürün tv menüde gösterilsin mi
    public bool IsActive { get; set; } = true; // ürün aktif mi değil mi
    public ProductOrderType Type { get; set; } // ürün sipariş türü (mutfak mı bar mı)
}
