namespace KufeArtFullAdission.Entity;

public sealed class CustomerPointsDbEntity : BaseDbEntity
{
    public Guid CustomerId { get; set; }
    public int TotalPoints { get; set; } = 0; // Mevcut puan bakiyesi
}
