using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface IMediaService
{
    Task<IReadOnlyList<FileRecordDto>> GetFilesAsync(CancellationToken ct = default);
    Task<FileRecordDto?> GetFileByIdAsync(Guid id, CancellationToken ct = default);
    Task<FileRecordDto> UploadFileAsync(Stream content, string fileName, string contentType, string uploadedBy, CancellationToken ct = default);
    Task<Stream> DownloadFileAsync(Guid id, CancellationToken ct = default);
    Task DeleteFileAsync(Guid id, CancellationToken ct = default);
}
