﻿namespace KufeArtFullAdission.Entity;

public sealed class ProductImagesDbEntity:BaseDbEntity
{
    public Guid ProductId { get; set; } // ürün id si
    public string ImagePath { get; set; } // ürün resmi yolu bir ürüne birden fazla resim eklenebilir
}
