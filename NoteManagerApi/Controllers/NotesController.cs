using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoteManagerApi.DTOs.Notes;
using NoteManagerApi.Entities;
using NoteManagerApi.Services;

namespace NoteManagerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INoteService noteService, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    // GET: api/notes
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetUserNotes()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notes = await _noteService.GetUserNotesAsync(userId);
            return Ok(notes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting user notes");
            return StatusCode(500, "An error occurred while getting notes");
        }
    }
    
    
    // GET: api/notes/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NoteDto>> GetNote(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var note = await _noteService.GetNoteByIdAsync(id, userId);
            return Ok(note);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting note {NoteId}", id);
            return StatusCode(500, "An error occurred while getting the note");
        }
    }
    
    // POST: api/notes
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NoteDto>> CreateNote(CreateNoteDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var note = await _noteService.CreateNoteAsync(dto, userId);

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while creating note");
            return StatusCode(500, "An error occurred while creating the note");
        }
    }
    
    // PUT: api/notes/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NoteDto>> UpdateNote(Guid id, UpdateNoteDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var note = await _noteService.UpdateNoteAsync(id, dto, userId);
            return Ok(note);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while updating note {NoteId}", id);
            return StatusCode(500, "An error occurred while updating the note");
        }
    }
    
    // DELETE: api/notes/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteNote(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _noteService.DeleteNoteAsync(id, userId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting note {NoteId}", id);
            return StatusCode(500, "An error occurred while deleting the note");
        }
    }
    
    // GET: api/notes/filter
    [HttpGet("filter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetFilteredNotes([FromQuery] NoteFilterDto filter)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notes = await _noteService.GetFilteredNotesAsync(filter, userId);
            return Ok(notes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while filtering notes");
            return StatusCode(500, "An error occurred while filtering notes");
        }
    }
}