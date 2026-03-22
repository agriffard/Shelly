namespace TenantHub.Core.DTOs;

public record AuditLogDto(
    long Id,
    string Actor,
    string Action,
    string EntityType,
    string EntityId,
    string? Detail,
    DateTime OccurredAt);
