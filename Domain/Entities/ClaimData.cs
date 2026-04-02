namespace Domain.Entities;

public record ClaimData()
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PhotoFileId { get; set; }
    public string? PhotoBase64 { get; set; }
}