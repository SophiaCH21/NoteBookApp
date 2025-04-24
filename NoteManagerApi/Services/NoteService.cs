using AutoMapper;
using NoteManagerApi.DTOs.Notes;
using NoteManagerApi.Entities;
using NoteManagerApi.Repositories;

namespace NoteManagerApi.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly IMapper _mapper;

    public NoteService(INoteRepository noteRepository, IMapper mapper)
    {
        _noteRepository = noteRepository;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<NoteDto>> GetUserNotesAsync(string userId)
    {
        var notes = await _noteRepository.GetAllAsync(userId);
        return _mapper.Map<IEnumerable<NoteDto>>(notes);
    }

    public async Task<NoteDto> GetNoteByIdAsync(Guid id, string userId)
    {
        var note = await _noteRepository.GetByIdAsync(id);
        
        if (note == null)
        {
            throw new KeyNotFoundException($"Заметка с ID {id} не найдена");
        }
        
        if (note.UserId != userId)
        {
            throw new UnauthorizedAccessException("У вас нет доступа к этой заметке");
        }

        return _mapper.Map<NoteDto>(note);
    }

    public async Task<NoteDto> CreateNoteAsync(CreateNoteDto dto, string userId)
    {
        var note = _mapper.Map<Note>(dto);
        note.UserId = userId;
        note.CreatedAt = DateTime.UtcNow;

        var createdNote = await _noteRepository.AddAsync(note);
        return _mapper.Map<NoteDto>(createdNote);
    }

    public async Task<NoteDto> UpdateNoteAsync(Guid id, UpdateNoteDto dto, string userId)
    {
        var existingNote = await _noteRepository.GetByIdAsync(id);
        
        if (existingNote == null)
        {
            throw new KeyNotFoundException($"Заметка с ID {id} не найдена");
        }
        
        if (existingNote.UserId != userId)
        {
            throw new UnauthorizedAccessException("У вас нет доступа к этой заметке");
        }

        _mapper.Map(dto, existingNote);
        existingNote.UpdatedAt = DateTime.UtcNow;

        await _noteRepository.UpdateAsync(existingNote);
        return _mapper.Map<NoteDto>(existingNote);
    }

    public async Task DeleteNoteAsync(Guid id, string userId)
    {
        var note = await _noteRepository.GetByIdAsync(id);
        
        if (note == null)
        {
            throw new KeyNotFoundException($"Заметка с ID {id} не найдена");
        }
        
        if (note.UserId != userId)
        {
            throw new UnauthorizedAccessException("У вас нет доступа к этой заметке");
        }

        await _noteRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<NoteDto>> GetFilteredNotesAsync(NoteFilterDto filter, string userId)
    {
        IEnumerable<Note> notes;

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            notes = await _noteRepository.SearchAsync(filter.SearchTerm, userId);
        }
        else if (filter.FromDate.HasValue && filter.ToDate.HasValue)
        {
            notes = await _noteRepository.FilterByDateAsync(
                filter.FromDate.Value,
                filter.ToDate.Value,
                userId);
        }
        else
        {
            notes = await _noteRepository.GetAllAsync(userId);
        }

        return _mapper.Map<IEnumerable<NoteDto>>(notes);
    }
}