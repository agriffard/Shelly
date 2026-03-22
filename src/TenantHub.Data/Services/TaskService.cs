using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class TaskService(TenantDbContext db) : ITaskService
{
    public async Task<IReadOnlyList<TaskItemDto>> GetTasksAsync(string? assignee = null, DateTime? dueBefore = null, DateTime? dueAfter = null, CancellationToken ct = default)
    {
        var q = db.TaskItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(assignee))
            q = q.Where(t => t.AssignedTo == assignee);
        if (dueBefore.HasValue)
            q = q.Where(t => t.DueDate <= dueBefore.Value);
        if (dueAfter.HasValue)
            q = q.Where(t => t.DueDate >= dueAfter.Value);

        return await q
            .OrderBy(t => t.DueDate)
            .Select(t => new TaskItemDto(t.Id, t.Title, t.Status, t.AssignedTo, t.DueDate, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TaskItemDto?> GetTaskByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.TaskItems.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TaskItemDto(t.Id, t.Title, t.Status, t.AssignedTo, t.DueDate, t.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TaskItemDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Status = TaskItemStatus.ToDo,
            AssignedTo = request.AssignedTo,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        db.TaskItems.Add(task);
        await db.SaveChangesAsync(ct);

        return new TaskItemDto(task.Id, task.Title, task.Status, task.AssignedTo, task.DueDate, task.CreatedAt);
    }

    public async Task<TaskItemDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await db.TaskItems.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Task {id} not found");

        if (request.Title is not null) task.Title = request.Title;
        if (request.Status.HasValue) task.Status = request.Status.Value;
        if (request.AssignedTo is not null) task.AssignedTo = request.AssignedTo;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate.Value;

        await db.SaveChangesAsync(ct);

        return new TaskItemDto(task.Id, task.Title, task.Status, task.AssignedTo, task.DueDate, task.CreatedAt);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.TaskItems.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Task {id} not found");

        db.TaskItems.Remove(task);
        await db.SaveChangesAsync(ct);
    }
}
