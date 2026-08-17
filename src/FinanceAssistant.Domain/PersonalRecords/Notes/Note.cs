using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.PersonalRecords.Notes;

public sealed class Note
{
    private Note(NoteId id, LocalProfileId profileId, string content, DateTimeOffset createdAt)
    {
        Id = id;
        ProfileId = profileId;
        Content = content;
        CreatedAt = createdAt;
    }

    public NoteId Id { get; }

    public LocalProfileId ProfileId { get; }

    public string Content { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Note Create(LocalProfileId profileId, string content, DateTimeOffset createdAt)
    {
        return Rehydrate(NoteId.New(), profileId, content, createdAt);
    }

    public static Note Rehydrate(
        NoteId id,
        LocalProfileId profileId,
        string content,
        DateTimeOffset createdAt)
    {
        return new Note(
            id,
            profileId,
            RequiredText.Normalize(content, "Note content"),
            createdAt);
    }
}
