namespace TenantHub.Core.DTOs;

public record NoteDto(
    Guid Id,
    string Title,
    string Body,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateNoteRequest(string Title, string Body, string CreatedBy);
public record UpdateNoteRequest(string Title, string Body);
