namespace TenantHub.Data.Models;

public class FileRecord
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = "";
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}
