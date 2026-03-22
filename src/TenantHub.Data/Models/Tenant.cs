namespace TenantHub.Data.Models;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public Core.DTOs.TenantStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }

    public ICollection<TenantFeature> Features { get; set; } = [];
}
