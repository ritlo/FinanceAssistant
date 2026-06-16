using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.Documents.ListDocuments;

public sealed class ListDocumentsUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IDocumentMetadataRepository documentRepository;

    public ListDocumentsUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IDocumentMetadataRepository documentRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.documentRepository = documentRepository;
    }

    public async Task<IReadOnlyList<DocumentResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var documents = await documentRepository.ListDocumentsAsync(profileId, cancellationToken);

        return documents
            .OrderByDescending(document => document.CreatedAt)
            .Select(DocumentResult.FromDocument)
            .ToArray();
    }
}
