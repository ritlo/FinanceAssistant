using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Domain.Tests.PersonalRecords.Notes;

public sealed class NoteTests
{
    [Fact]
    public void CreateRejectsEmptyContent()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Note.Create(LocalProfileId.New(), " \t\r\n ", DateTimeOffset.UtcNow));

        Assert.Equal("Note content is required.", exception.Message);
    }

    [Fact]
    public void CreateNormalizesContentWhitespace()
    {
        var note = Note.Create(
            LocalProfileId.New(),
            "  first\tsecond\r\nthird  ",
            DateTimeOffset.UtcNow);

        Assert.Equal("first second third", note.Content);
    }

    [Fact]
    public void RehydratePreservesIdentityProfileAndTimestamp()
    {
        var id = NoteId.New();
        var profileId = LocalProfileId.New();
        var createdAt = new DateTimeOffset(2026, 6, 16, 10, 30, 0, TimeSpan.Zero);

        var note = Note.Rehydrate(id, profileId, "Saved note", createdAt);

        Assert.Equal(id, note.Id);
        Assert.Equal(profileId, note.ProfileId);
        Assert.Equal(createdAt, note.CreatedAt);
        Assert.Equal("Saved note", note.Content);
    }
}
