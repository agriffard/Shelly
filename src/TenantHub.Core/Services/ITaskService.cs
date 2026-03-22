using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(string? assignee = null, DateTime? dueBefore = null, DateTime? dueAfter = null, CancellationToken ct = default);
    Task<TaskItemDto?> GetTaskByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskItemDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteTaskAsync(Guid id, CancellationToken ct = default);
}
