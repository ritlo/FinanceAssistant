using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Application.PersonalRecords.Notes;

public interface INoteRepository
{
    Task AddNoteAsync(Note note, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> ListNotesAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default);

    Task<Note?> GetNoteAsync(
        LocalProfileId profileId,
        NoteId noteId,
        CancellationToken cancellationToken = default);

    Task DeleteNoteAsync(
        LocalProfileId profileId,
        NoteId noteId,
        CancellationToken cancellationToken = default);
}
