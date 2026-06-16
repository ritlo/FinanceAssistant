using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Application.PersonalRecords.Notes.CreateNote;

public sealed class CreateNoteUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly INoteRepository noteRepository;
    private readonly IClock clock;

    public CreateNoteUseCase(
        ICurrentProfileProvider currentProfileProvider,
        INoteRepository noteRepository,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.noteRepository = noteRepository;
        this.clock = clock;
    }

    public async Task<NoteResult> ExecuteAsync(
        CreateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var note = Note.Create(profileId, request.Content, clock.UtcNow);

        await noteRepository.AddNoteAsync(note, cancellationToken);

        return NoteResult.FromNote(note);
    }
}
