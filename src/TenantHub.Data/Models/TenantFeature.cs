namespace TenantHub.Data.Models;

public class TenantFeature
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FeatureName { get; set; } = "";
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
