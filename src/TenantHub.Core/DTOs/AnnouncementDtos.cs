namespace TenantHub.Core.DTOs;

public record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPinned,
    DateTime PublishedAt,
    DateTime ExpiresAt);

public record CreateAnnouncementRequest(string Title, string Body, bool IsPinned, DateTime ExpiresAt);
public record UpdateAnnouncementRequest(string? Title = null, string? Body = null, bool? IsPinned = null, DateTime? ExpiresAt = null);
