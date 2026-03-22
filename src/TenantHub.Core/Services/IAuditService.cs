using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface IAuditService
{
    Task LogAsync(string actor, string action, string entityType, string entityId, string? detail = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken ct = default);
}

public record AuditLogQuery(
    int Page = 1,
    int PageSize = 50,
    string? Actor = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null);
