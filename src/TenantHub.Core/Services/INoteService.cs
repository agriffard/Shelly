using TenantHub.Core.DTOs;

namespace TenantHub.Core.Services;

public interface INoteService
{
    Task<IReadOnlyList<NoteDto>> GetNotesAsync(string? searchTitle = null, CancellationToken ct = default);
    Task<NoteDto?> GetNoteByIdAsync(Guid id, CancellationToken ct = default);
    Task<NoteDto> CreateNoteAsync(CreateNoteRequest request, CancellationToken ct = default);
    Task<NoteDto> UpdateNoteAsync(Guid id, UpdateNoteRequest request, CancellationToken ct = default);
    Task DeleteNoteAsync(Guid id, CancellationToken ct = default);
}
