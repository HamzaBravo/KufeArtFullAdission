using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class ProductDbEntity:BaseDbEntity
{
    public string CategoryName { get; set; }
    public string Name { get; set; } // ürün adı
    public string Description { get; set; } // ürün açıklaması opsiyonel
    public double Price { get; set; } // ürün birim fiyatı
    public bool IsQrMenu { get; set; } // ürün qr menüde gösterilsin mi
    public bool IsTvMenu { get; set; } // ürün tv menüde gösterilsin mi
    public bool IsActive { get; set; } = true; // ürün aktif mi değil mi
    public ProductOrderType Type { get; set; } // ürün sipariş türü (mutfak mı bar mı)

    public bool HasCampaign { get; set; } = false; // QR menüde kampanyalı gösterim
    public string CampaignCaption { get; set; } // "10+1"
    public string CampaignDetail { get; set; } // "1 ay içerisinde 10 kahve alana 1 kahve hediye"
}
