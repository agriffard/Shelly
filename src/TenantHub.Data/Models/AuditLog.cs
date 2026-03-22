namespace TenantHub.Data.Models;

public class AuditLog
{
    public long Id { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
}
