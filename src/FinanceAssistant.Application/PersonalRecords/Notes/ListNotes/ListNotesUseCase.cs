using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.PersonalRecords.Notes.ListNotes;

public sealed class ListNotesUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly INoteRepository noteRepository;

    public ListNotesUseCase(
        ICurrentProfileProvider currentProfileProvider,
        INoteRepository noteRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.noteRepository = noteRepository;
    }

    public async Task<IReadOnlyList<NoteResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var notes = await noteRepository.ListNotesAsync(profileId, cancellationToken);

        return notes
            .OrderByDescending(note => note.CreatedAt)
            .Select(NoteResult.FromNote)
            .ToArray();
    }
}
