namespace KufeArtFullAdission.GarsonMvc.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class MoveTableRequest
    {
        public Guid SourceTableId { get; set; }
        public Guid TargetTableId { get; set; }
    }

    public class MergeTablesRequest
    {
        public Guid SourceTableId { get; set; }
        public Guid TargetTableId { get; set; }
    }

    public class CancelOrderRequest
    {
        public Guid TableId { get; set; }
    }
}
