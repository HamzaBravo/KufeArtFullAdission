namespace KufeArtFullAdission.Entity;

public sealed class OrderBatchStatusDbEntity : BaseDbEntity
{
    public Guid OrderBatchId { get; set; } // AddtionHistoryDbEntity.OrderBatchId referansı
    public bool IsReady { get; set; } = false; // false = Hazırlanıyor, true = Hazır
    public string CompletedBy { get; set; } // Hangi tablet/garson hazırladı
    public string Department { get; set; } // "Kitchen" veya "Bar"
    public DateTime? CompletedAt { get; set; } // Hazır olduğu zaman
}
