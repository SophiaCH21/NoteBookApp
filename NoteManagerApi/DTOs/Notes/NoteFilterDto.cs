namespace NoteManagerApi.DTOs.Notes;

public class NoteFilterDto
{
    public string SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

}