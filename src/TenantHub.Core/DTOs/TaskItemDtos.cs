namespace TenantHub.Core.DTOs;

public record TaskItemDto(
    Guid Id,
    string Title,
    TaskItemStatus Status,
    string AssignedTo,
    DateTime DueDate,
    DateTime CreatedAt);

public enum TaskItemStatus
{
    ToDo,
    InProgress,
    Done
}

public record CreateTaskRequest(string Title, string AssignedTo, DateTime DueDate);
public record UpdateTaskRequest(string? Title = null, TaskItemStatus? Status = null, string? AssignedTo = null, DateTime? DueDate = null);
