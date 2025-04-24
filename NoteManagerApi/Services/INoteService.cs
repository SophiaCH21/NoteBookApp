using NoteManagerApi.DTOs.Notes;

namespace NoteManagerApi.Services;

public interface INoteService
{
        Task<IEnumerable<NoteDto>> GetUserNotesAsync(string userId);
        Task<NoteDto> GetNoteByIdAsync(Guid id, string userId);
        Task<NoteDto> CreateNoteAsync(CreateNoteDto dto, string userId);
        Task<NoteDto> UpdateNoteAsync(Guid id, UpdateNoteDto dto, string userId);
        Task DeleteNoteAsync(Guid id, string userId);
        Task<IEnumerable<NoteDto>> GetFilteredNotesAsync(NoteFilterDto filter, string userId);
}