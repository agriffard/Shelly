using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class MediaService(TenantDbContext db, ICurrentTenantService tenantService) : IMediaService
{
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    private string StorageRoot => Path.Combine("uploads", tenantService.TenantSlug);

    public async Task<IReadOnlyList<FileRecordDto>> GetFilesAsync(CancellationToken ct = default)
    {
        return await db.FileRecords.AsNoTracking()
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new FileRecordDto(f.Id, f.FileName, f.ContentType, f.SizeBytes, f.UploadedBy, f.UploadedAt))
            .ToListAsync(ct);
    }

    public async Task<FileRecordDto?> GetFileByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.FileRecords.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FileRecordDto(f.Id, f.FileName, f.ContentType, f.SizeBytes, f.UploadedBy, f.UploadedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<FileRecordDto> UploadFileAsync(Stream content, string fileName, string contentType, string uploadedBy, CancellationToken ct = default)
    {
        Directory.CreateDirectory(StorageRoot);

        var fileId = Guid.NewGuid();
        var storagePath = Path.Combine(StorageRoot, $"{fileId}_{fileName}");

        await using var fileStream = File.Create(storagePath);
        await content.CopyToAsync(fileStream, ct);
        var sizeBytes = fileStream.Length;

        if (sizeBytes > MaxFileSizeBytes)
        {
            fileStream.Close();
            File.Delete(storagePath);
            throw new InvalidOperationException($"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        var record = new FileRecord
        {
            Id = fileId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        db.FileRecords.Add(record);
        await db.SaveChangesAsync(ct);

        return new FileRecordDto(record.Id, record.FileName, record.ContentType, record.SizeBytes, record.UploadedBy, record.UploadedAt);
    }

    public async Task<Stream> DownloadFileAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.FileRecords.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"File {id} not found");

        return File.OpenRead(record.StoragePath);
    }

    public async Task DeleteFileAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.FileRecords.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"File {id} not found");

        if (File.Exists(record.StoragePath))
            File.Delete(record.StoragePath);

        db.FileRecords.Remove(record);
        await db.SaveChangesAsync(ct);
    }
}
