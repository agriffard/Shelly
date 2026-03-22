using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class AnnouncementService(TenantDbContext db) : IAnnouncementService
{
    public async Task<IReadOnlyList<AnnouncementDto>> GetAnnouncementsAsync(CancellationToken ct = default)
    {
        return await db.Announcements.AsNoTracking()
            .Where(a => a.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.IsPinned, a.PublishedAt, a.ExpiresAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AnnouncementDto>> GetPinnedAsync(CancellationToken ct = default)
    {
        return await db.Announcements.AsNoTracking()
            .Where(a => a.IsPinned && a.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.IsPinned, a.PublishedAt, a.ExpiresAt))
            .ToListAsync(ct);
    }

    public async Task<AnnouncementDto?> GetAnnouncementByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Announcements.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.IsPinned, a.PublishedAt, a.ExpiresAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken ct = default)
    {
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            IsPinned = request.IsPinned,
            PublishedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);

        return new AnnouncementDto(announcement.Id, announcement.Title, announcement.Body, announcement.IsPinned, announcement.PublishedAt, announcement.ExpiresAt);
    }

    public async Task<AnnouncementDto> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementRequest request, CancellationToken ct = default)
    {
        var announcement = await db.Announcements.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Announcement {id} not found");

        if (request.Title is not null) announcement.Title = request.Title;
        if (request.Body is not null) announcement.Body = request.Body;
        if (request.IsPinned.HasValue) announcement.IsPinned = request.IsPinned.Value;
        if (request.ExpiresAt.HasValue) announcement.ExpiresAt = request.ExpiresAt.Value;

        await db.SaveChangesAsync(ct);

        return new AnnouncementDto(announcement.Id, announcement.Title, announcement.Body, announcement.IsPinned, announcement.PublishedAt, announcement.ExpiresAt);
    }

    public async Task DeleteAnnouncementAsync(Guid id, CancellationToken ct = default)
    {
        var announcement = await db.Announcements.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Announcement {id} not found");

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync(ct);
    }
}
