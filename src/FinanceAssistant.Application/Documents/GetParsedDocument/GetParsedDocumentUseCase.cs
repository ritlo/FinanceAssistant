using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents.GetParsedDocument;

public sealed class GetParsedDocumentUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IDocumentParsedContentRepository parsedContentRepository;

    public GetParsedDocumentUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IDocumentParsedContentRepository parsedContentRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.parsedContentRepository = parsedContentRepository;
    }

    public async Task<ParsedDocumentResult?> ExecuteAsync(
        GetParsedDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var parsedDocument = await parsedContentRepository.GetParsedDocumentAsync(
            profileId,
            new DocumentId(request.DocumentId),
            cancellationToken);

        return parsedDocument is null ? null : ParsedDocumentResult.FromParsedDocument(parsedDocument);
    }
}
