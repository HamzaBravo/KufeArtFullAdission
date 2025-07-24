namespace KufeArtFullAdission.Entity;

public sealed class CustomerDbEntity:BaseDbEntity
{
    public string Fullname { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}
