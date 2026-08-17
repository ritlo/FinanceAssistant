using System.Globalization;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class NoteDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    public static NoteDocument FromNote(Note note)
    {
        return new NoteDocument
        {
            Id = note.Id.Value,
            ProfileId = note.ProfileId.Value,
            Content = note.Content,
            CreatedAt = note.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
        };
    }

    public Note ToNote()
    {
        return Note.Rehydrate(
            new NoteId(Id),
            new LocalProfileId(ProfileId),
            Content,
            DateTimeOffset.ParseExact(CreatedAt, "O", CultureInfo.InvariantCulture));
    }
}
