using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.PersonalRecords.Notes;

namespace FinanceAssistant.Infrastructure.IntegrationTests.PersonalRecords.Notes;

[Collection("Sequential")]
public sealed class LiteDbNoteRepositoryTests
{
    [Fact]
    public async Task CreateAndListPersistNotes()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbNoteRepository(options);
        var createdAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var note = Note.Create(profileId, "Persisted note", createdAt);

        await repository.AddNoteAsync(note);

        var notes = await repository.ListNotesAsync(profileId);
        var persisted = Assert.Single(notes);
        Assert.Equal(note.Id, persisted.Id);
        Assert.Equal(profileId, persisted.ProfileId);
        Assert.Equal("Persisted note", persisted.Content);
        Assert.Equal(createdAt, persisted.CreatedAt);
    }

    [Fact]
    public async Task DeleteRemovesOnlyCurrentProfileNote()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var repository = new LiteDbNoteRepository(options);
        var currentNote = Note.Create(profileId, "Current", DateTimeOffset.UtcNow);
        var otherNote = Note.Create(otherProfileId, "Other", DateTimeOffset.UtcNow);
        await repository.AddNoteAsync(currentNote);
        await repository.AddNoteAsync(otherNote);

        await repository.DeleteNoteAsync(profileId, currentNote.Id);
        await repository.DeleteNoteAsync(profileId, otherNote.Id);

        Assert.Empty(await repository.ListNotesAsync(profileId));
        var otherNotes = await repository.ListNotesAsync(otherProfileId);
        Assert.Equal(otherNote.Id, Assert.Single(otherNotes).Id);
    }
}
