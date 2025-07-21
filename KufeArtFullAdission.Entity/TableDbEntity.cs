namespace KufeArtFullAdission.Entity;

public sealed class TableDbEntity:BaseDbEntity
{
    public Guid? AddionStatus { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }
}
