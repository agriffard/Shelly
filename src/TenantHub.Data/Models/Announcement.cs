namespace TenantHub.Data.Models;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsPinned { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
