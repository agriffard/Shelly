using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface ITenantManagementService
{
    Task<PagedResult<TenantDto>> GetTenantsAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<TenantDto?> GetTenantByIdAsync(Guid id, CancellationToken ct = default);
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string actor, CancellationToken ct = default);
    Task<TenantDto> UpdateTenantAsync(Guid id, UpdateTenantRequest request, string actor, CancellationToken ct = default);
    Task SetTenantStatusAsync(Guid id, TenantStatus status, string actor, CancellationToken ct = default);
    Task DeleteTenantAsync(Guid id, string actor, CancellationToken ct = default);
    Task ToggleFeatureAsync(Guid tenantId, string featureName, bool enabled, string actor, CancellationToken ct = default);
    Task<IReadOnlyList<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId, CancellationToken ct = default);
}
