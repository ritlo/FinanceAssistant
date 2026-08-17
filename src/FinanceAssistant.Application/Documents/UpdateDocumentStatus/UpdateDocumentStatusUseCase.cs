using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents.UpdateDocumentStatus;

public sealed class UpdateDocumentStatusUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IDocumentMetadataRepository documentRepository;
    private readonly IClock clock;

    public UpdateDocumentStatusUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IDocumentMetadataRepository documentRepository,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.documentRepository = documentRepository;
        this.clock = clock;
    }

    public async Task<DocumentResult> ExecuteAsync(
        UpdateDocumentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var document = await documentRepository.GetDocumentAsync(
            profileId,
            new DocumentId(request.Id),
            cancellationToken);

        if (document is null)
        {
            throw new DomainValidationException("Document was not found.");
        }

        switch (request.ParseStatus)
        {
            case DocumentParseStatus.Processing:
                document.MarkProcessing(clock.UtcNow);
                break;
            case DocumentParseStatus.Completed:
                document.MarkCompleted(clock.UtcNow);
                break;
            case DocumentParseStatus.Failed:
                document.MarkFailed(request.FailureReason ?? string.Empty, clock.UtcNow);
                break;
            case DocumentParseStatus.Pending:
            default:
                throw new DomainValidationException("Document parse status transition is not supported.");
        }

        await documentRepository.UpdateDocumentAsync(document, cancellationToken);

        return DocumentResult.FromDocument(document);
    }
}
