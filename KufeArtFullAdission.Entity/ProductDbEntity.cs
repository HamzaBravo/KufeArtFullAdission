using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class ProductDbEntity:BaseDbEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public bool IsQrMenu { get; set; }
    public bool IsTvMenu { get; set; }
    public bool IsActive { get; set; } = true;
    public ProductOrderType Type { get; set; }
}
