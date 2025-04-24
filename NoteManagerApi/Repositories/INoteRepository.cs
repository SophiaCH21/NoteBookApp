using NoteManagerApi.Entities;

namespace NoteManagerApi.Repositories;

public interface INoteRepository
{
    Task<Note> GetByIdAsync(Guid id);
    Task<IEnumerable<Note>> GetAllAsync(string userId);
    Task<Note> AddAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Note>> SearchAsync(string searchTerm, string userId);
    Task<IEnumerable<Note>> FilterByDateAsync(DateTime fromDate, DateTime toDate, string userId);
}