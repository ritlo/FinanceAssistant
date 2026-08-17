using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Notes;
using FinanceAssistant.Application.PersonalRecords.Notes.CreateNote;
using FinanceAssistant.Application.PersonalRecords.Notes.DeleteNote;
using FinanceAssistant.Application.PersonalRecords.Notes.ListNotes;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Application.Tests.PersonalRecords.Notes;

public sealed class NoteUseCaseTests
{
    [Fact]
    public async Task CreateUsesCurrentProfileAndClock()
    {
        var profileId = LocalProfileId.New();
        var createdAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeNoteRepository();
        var useCase = new CreateNoteUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository,
            new FixedClock(createdAt));

        var result = await useCase.ExecuteAsync(new CreateNoteRequest("  remember this  "));

        var note = repository.Notes.Single();
        Assert.Equal(profileId, note.ProfileId);
        Assert.Equal(createdAt, note.CreatedAt);
        Assert.Equal("remember this", note.Content);
        Assert.Equal(note.Id.Value, result.Id);
        Assert.Equal(createdAt, result.CreatedAt);
    }

    [Fact]
    public async Task ListSortsNewestFirst()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeNoteRepository();
        var older = repository.AddExisting(profileId, "Older", new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero));
        var newer = repository.AddExisting(profileId, "Newer", new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.Zero));
        var useCase = new ListNotesUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var result = await useCase.ExecuteAsync();

        Assert.Collection(
            result,
            note => Assert.Equal(newer.Id.Value, note.Id),
            note => Assert.Equal(older.Id.Value, note.Id));
    }

    [Fact]
    public async Task ListExcludesOtherProfileNotes()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeNoteRepository();
        var current = repository.AddExisting(profileId, "Current", DateTimeOffset.UtcNow);
        repository.AddExisting(otherProfileId, "Other", DateTimeOffset.UtcNow);
        var useCase = new ListNotesUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var result = await useCase.ExecuteAsync();

        var note = Assert.Single(result);
        Assert.Equal(current.Id.Value, note.Id);
    }

    [Fact]
    public async Task DeleteRemovesCurrentProfileNote()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeNoteRepository();
        var current = repository.AddExisting(profileId, "Current", DateTimeOffset.UtcNow);
        var other = repository.AddExisting(otherProfileId, "Other", DateTimeOffset.UtcNow);
        var useCase = new DeleteNoteUseCase(new FixedCurrentProfileProvider(profileId), repository);

        await useCase.ExecuteAsync(new DeleteNoteRequest(current.Id.Value));

        Assert.Empty(await repository.ListNotesAsync(profileId));
        var otherNotes = await repository.ListNotesAsync(otherProfileId);
        Assert.Equal(other.Id, Assert.Single(otherNotes).Id);
    }

    [Fact]
    public async Task DeleteRejectsMissingOrOtherProfileNote()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeNoteRepository();
        var other = repository.AddExisting(otherProfileId, "Other", DateTimeOffset.UtcNow);
        var useCase = new DeleteNoteUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var missingException = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new DeleteNoteRequest(Guid.NewGuid())));
        var otherException = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new DeleteNoteRequest(other.Id.Value)));

        Assert.Equal("Note was not found.", missingException.Message);
        Assert.Equal("Note was not found.", otherException.Message);
    }

    private sealed class FixedCurrentProfileProvider : ICurrentProfileProvider
    {
        private readonly LocalProfileId profileId;

        public FixedCurrentProfileProvider(LocalProfileId profileId)
        {
            this.profileId = profileId;
        }

        public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(profileId);
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeNoteRepository : INoteRepository
    {
        public List<Note> Notes { get; } = [];

        public Note AddExisting(LocalProfileId profileId, string content, DateTimeOffset createdAt)
        {
            var note = Note.Create(profileId, content, createdAt);
            Notes.Add(note);
            return note;
        }

        public Task AddNoteAsync(Note note, CancellationToken cancellationToken = default)
        {
            Notes.Add(note);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Note>> ListNotesAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Note>>(
                Notes.Where(note => note.ProfileId == profileId).ToArray());
        }

        public Task<Note?> GetNoteAsync(
            LocalProfileId profileId,
            NoteId noteId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Note?>(
                Notes.SingleOrDefault(note => note.ProfileId == profileId && note.Id == noteId));
        }

        public Task DeleteNoteAsync(
            LocalProfileId profileId,
            NoteId noteId,
            CancellationToken cancellationToken = default)
        {
            var note = Notes.SingleOrDefault(candidate => candidate.ProfileId == profileId && candidate.Id == noteId);
            if (note is not null)
            {
                Notes.Remove(note);
            }

            return Task.CompletedTask;
        }
    }
}
