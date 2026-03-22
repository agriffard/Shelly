namespace TenantHub.Core.Services;

public interface ICurrentTenantService
{
    string TenantSlug { get; }
    string TenantSchema { get; }
}
