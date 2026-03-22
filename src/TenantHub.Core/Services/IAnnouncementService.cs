using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementDto>> GetAnnouncementsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AnnouncementDto>> GetPinnedAsync(CancellationToken ct = default);
    Task<AnnouncementDto?> GetAnnouncementByIdAsync(Guid id, CancellationToken ct = default);
    Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequest request, CancellationToken ct = default);
    Task<AnnouncementDto> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementRequest request, CancellationToken ct = default);
    Task DeleteAnnouncementAsync(Guid id, CancellationToken ct = default);
}
