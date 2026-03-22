namespace TenantHub.Core.DTOs;

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime? SuspendedAt,
    int EnabledFeatureCount);

public record CreateTenantRequest(
    string Name,
    string Slug,
    string? Description);

public record UpdateTenantRequest(
    string Name,
    string? Description);

public record TenantFeatureDto(
    Guid Id,
    string FeatureName,
    bool IsEnabled,
    DateTime UpdatedAt);
