namespace TenantHub.Core.DTOs;

public record FeatureInfo(
    string Name,
    string DisplayName,
    string Description,
    string[] DependsOn);
