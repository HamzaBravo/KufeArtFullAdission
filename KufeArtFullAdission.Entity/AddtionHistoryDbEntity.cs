namespace KufeArtFullAdission.Entity;

public sealed class AddtionHistoryDbEntity:BaseDbEntity
{
    public Guid AddionStatusId { get; set; } // hangi masaya sipariş verildiğini gösterir. Bu masanın sipariş id'si
    public string ShorLabel { get; set; }// bir masada birden fazla kişi oturuyor olabilir. Ve hesapları ayrı ödemek isteyebilirler bu yüzden siparişlere kişi adının etiketi girilebilir opsiyonel
    public string ProductName { get; set; } // ürün adı
    public double ProductPrice { get; set; } // ürün birim fiyatı
    public int ProductQuantity { get; set; } // ürün adedi
    public double TotalPrice { get; set; } // toplam fiyat
    public Guid PersonId { get; set; } // siparişi veren kişinin id'si
    public string PersonFullName { get; set; } // siparişi veren kişinin tam adı
}
