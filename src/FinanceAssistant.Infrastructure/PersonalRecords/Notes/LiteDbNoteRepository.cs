using FinanceAssistant.Application.PersonalRecords.Notes;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.PersonalRecords.Notes;

public sealed class LiteDbNoteRepository : INoteRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbNoteRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task AddNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<NoteDocument>(LiteDbCollectionNames.Notes, BsonAutoId.Guid)
            .Insert(NoteDocument.FromNote(note));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Note>> ListNotesAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var notes = database
            .GetCollection<NoteDocument>(LiteDbCollectionNames.Notes, BsonAutoId.Guid)
            .FindAll()
            .Where(note => note.ProfileId == profileId.Value)
            .Select(note => note.ToNote())
            .ToArray();

        return Task.FromResult<IReadOnlyList<Note>>(notes);
    }

    public Task<Note?> GetNoteAsync(
        LocalProfileId profileId,
        NoteId noteId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<NoteDocument>(LiteDbCollectionNames.Notes, BsonAutoId.Guid)
            .FindById(noteId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<Note?>(null);
        }

        return Task.FromResult<Note?>(document.ToNote());
    }

    public Task DeleteNoteAsync(
        LocalProfileId profileId,
        NoteId noteId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<NoteDocument>(LiteDbCollectionNames.Notes, BsonAutoId.Guid)
            .FindById(noteId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<NoteDocument>(LiteDbCollectionNames.Notes, BsonAutoId.Guid)
            .Delete(noteId.Value);

        return Task.CompletedTask;
    }
}
