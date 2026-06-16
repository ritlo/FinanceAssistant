using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.PersonalRecords.Notes;

namespace FinanceAssistant.Application.PersonalRecords.Notes.DeleteNote;

public sealed class DeleteNoteUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly INoteRepository noteRepository;

    public DeleteNoteUseCase(
        ICurrentProfileProvider currentProfileProvider,
        INoteRepository noteRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.noteRepository = noteRepository;
    }

    public async Task ExecuteAsync(
        DeleteNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var noteId = new NoteId(request.Id);
        var note = await noteRepository.GetNoteAsync(profileId, noteId, cancellationToken);

        if (note is null)
        {
            throw new DomainValidationException("Note was not found.");
        }

        await noteRepository.DeleteNoteAsync(profileId, noteId, cancellationToken);
    }
}
