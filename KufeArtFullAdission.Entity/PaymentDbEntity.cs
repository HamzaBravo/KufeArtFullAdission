using KufeArtFullAdission.Enums;

namespace KufeArtFullAdission.Entity;

public sealed class PaymentDbEntity:BaseDbEntity
{
    public Guid TableId { get; set; } // masa numarası
    public Guid AddionStatusId { get; set; } // adisyon numarası 
    public PaymentType PaymentType { get; set; } //ödeme yönetimi
    public double Amount { get; set; } // ödeme tutarı
    public  string ShortLabel { get; set; } // opsiyonel etiket için
    public Guid PersonId { get; set; } // işlem yapan kullanıcı
}
