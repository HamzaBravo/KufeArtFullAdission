namespace KufeArtFullAdission.Entity;

public class CustomerDbEntity
{
    public string Fullname { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int TotalVisits { get; set; } = 0;
}
