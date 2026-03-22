using TenantHub.Core.DTOs;

namespace TenantHub.Data.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public TaskItemStatus Status { get; set; }
    public string AssignedTo { get; set; } = "";
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
