using TenantHub.Core.Services;

namespace TenantHub.Data.Services;

public class CurrentTenantService(string tenantSlug, string tenantSchema) : ICurrentTenantService
{
    public string TenantSlug => tenantSlug;
    public string TenantSchema => tenantSchema;
}
