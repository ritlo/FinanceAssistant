using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Application.PersonalRecords.Notes;

public sealed record NoteResult(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt)
{
    public static NoteResult FromNote(Note note)
    {
        return new NoteResult(note.Id.Value, note.Content, note.CreatedAt);
    }
}
