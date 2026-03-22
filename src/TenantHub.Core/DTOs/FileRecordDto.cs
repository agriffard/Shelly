namespace TenantHub.Core.DTOs;

public record FileRecordDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedBy,
    DateTime UploadedAt);
