using Microsoft.EntityFrameworkCore;
using NoteManagerApi.Data;
using NoteManagerApi.Entities;

namespace NoteManagerApi.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly ApplicationDbContext _context;

    public NoteRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }
    
    public async Task<Note> GetByIdAsync(Guid id)
    {
        return await _context.Notes.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Note>> GetAllAsync(string userId)
    {
        return await _context.Notes.Where(n => n.UserId == userId).ToListAsync();
    }

    public async Task<Note> AddAsync(Note note)
    {
        await _context.Notes.AddAsync(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task UpdateAsync(Note note)
    {
        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var note = await GetByIdAsync(id);
        if (note != null)
        {
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Note>> SearchAsync(string searchTerm, string userId)
    {
        return await _context.Notes
            .Where(n => n.UserId == userId &&
                        (n.Title.Contains(searchTerm) || n.Description.Contains(searchTerm)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Note>> FilterByDateAsync(DateTime fromDate, DateTime toDate, string userId)
    {
        //TODO Возможно нужно по UpdateDate
        return await _context.Notes
            .Where(n => n.UserId == userId &&
                        n.CreatedAt >= fromDate &&
                        n.CreatedAt <= toDate)
            .ToListAsync();
    }
}