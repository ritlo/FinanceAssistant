using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents.CreateDocumentRecord;

public sealed class CreateDocumentRecordUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IDocumentMetadataRepository documentRepository;
    private readonly IDocumentTemporaryStorage temporaryStorage;
    private readonly IClock clock;

    public CreateDocumentRecordUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IDocumentMetadataRepository documentRepository,
        IDocumentTemporaryStorage temporaryStorage,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.documentRepository = documentRepository;
        this.temporaryStorage = temporaryStorage;
        this.clock = clock;
    }

    public async Task<DocumentResult> ExecuteAsync(
        CreateDocumentRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        TemporaryDocumentFile? temporaryFile = null;
        try
        {
            temporaryFile = await temporaryStorage.SaveAsync(request.Content, cancellationToken);
            var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
            var hash = await temporaryStorage.ComputeSha256HashAsync(temporaryFile, cancellationToken);
            var document = UploadedDocument.Create(
                profileId,
                ExtractDisplayName(request.OriginalDisplayName),
                request.VerifiedMediaType,
                temporaryFile.ByteLength,
                hash,
                clock.UtcNow);

            await documentRepository.AddDocumentAsync(document, cancellationToken);

            return DocumentResult.FromDocument(document);
        }
        finally
        {
            if (temporaryFile is not null)
            {
                await temporaryStorage.DeleteAsync(temporaryFile, CancellationToken.None);
            }
        }
    }

    private static string ExtractDisplayName(string originalDisplayName)
    {
        var normalizedSeparators = originalDisplayName.Replace('\\', '/');
        var segments = normalizedSeparators.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length == 0 ? originalDisplayName : segments[^1];
    }
}
