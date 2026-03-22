using Microsoft.EntityFrameworkCore;
using TenantHub.Core.DTOs;
using TenantHub.Core.Services;
using TenantHub.Data.Models;

namespace TenantHub.Data.Services;

public class NoteService(TenantDbContext db) : INoteService
{
    public async Task<IReadOnlyList<NoteDto>> GetNotesAsync(string? searchTitle = null, CancellationToken ct = default)
    {
        var q = db.Notes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(searchTitle))
            q = q.Where(n => n.Title.Contains(searchTitle));

        return await q
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteDto(n.Id, n.Title, n.Body, n.CreatedBy, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<NoteDto?> GetNoteByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Notes.AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new NoteDto(n.Id, n.Title, n.Body, n.CreatedBy, n.CreatedAt, n.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<NoteDto> CreateNoteAsync(CreateNoteRequest request, CancellationToken ct = default)
    {
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Body = request.Body,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

        return new NoteDto(note.Id, note.Title, note.Body, note.CreatedBy, note.CreatedAt, note.UpdatedAt);
    }

    public async Task<NoteDto> UpdateNoteAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default)
    {
        var note = await db.Notes.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Note {id} not found");

        note.Title = request.Title;
        note.Body = request.Body;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new NoteDto(note.Id, note.Title, note.Body, note.CreatedBy, note.CreatedAt, note.UpdatedAt);
    }

    public async Task DeleteNoteAsync(Guid id, CancellationToken ct = default)
    {
        var note = await db.Notes.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Note {id} not found");

        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
    }
}
