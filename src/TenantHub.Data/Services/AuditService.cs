using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class AuditService(AdminDbContext db) : IAuditService
{
    public async Task LogAsync(string actor, string action, string entityType, string entityId, string? detail = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Actor = actor,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            OccurredAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(query.Actor))
            q = q.Where(a => a.Actor == query.Actor);
        if (!string.IsNullOrEmpty(query.Action))
            q = q.Where(a => a.Action == query.Action);
        if (query.From.HasValue)
            q = q.Where(a => a.OccurredAt >= query.From.Value);
        if (query.To.HasValue)
            q = q.Where(a => a.OccurredAt <= query.To.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.Actor, a.Action, a.EntityType, a.EntityId, a.Detail, a.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(items, totalCount, query.Page, query.PageSize);
    }
}
