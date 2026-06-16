using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents.ParseDocument;

public sealed class ParseDocumentUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IDocumentMetadataRepository documentRepository;
    private readonly IDocumentParsedContentRepository parsedContentRepository;
    private readonly IDocumentTemporaryStorage temporaryStorage;
    private readonly IDocumentParser parser;
    private readonly IClock clock;

    public ParseDocumentUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IDocumentMetadataRepository documentRepository,
        IDocumentParsedContentRepository parsedContentRepository,
        IDocumentTemporaryStorage temporaryStorage,
        IDocumentParser parser,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.documentRepository = documentRepository;
        this.parsedContentRepository = parsedContentRepository;
        this.temporaryStorage = temporaryStorage;
        this.parser = parser;
        this.clock = clock;
    }

    public async Task<ParseDocumentResult> ExecuteAsync(
        ParseDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Content);

        TemporaryDocumentFile? temporaryFile = null;
        UploadedDocument? document = null;

        try
        {
            temporaryFile = await temporaryStorage.SaveAsync(request.Content, cancellationToken);
            var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
            var hash = await temporaryStorage.ComputeSha256HashAsync(temporaryFile, cancellationToken);
            document = UploadedDocument.Create(
                profileId,
                ExtractDisplayName(request.OriginalDisplayName),
                request.DeclaredMediaType,
                temporaryFile.ByteLength,
                hash,
                clock.UtcNow);

            await documentRepository.AddDocumentAsync(document, cancellationToken);

            document.MarkProcessing(clock.UtcNow);
            await documentRepository.UpdateDocumentAsync(document, cancellationToken);

            await using var content = await temporaryStorage.OpenReadAsync(temporaryFile, cancellationToken);
            var parsed = await parser.ParseAsync(content, request.DeclaredMediaType, cancellationToken);
            var parsedDocument = ParsedDocument.Create(
                document.Id,
                profileId,
                parsed.VerifiedMediaType,
                parsed.UntrustedExtractedText,
                parsed.PdfPageCount,
                clock.UtcNow);

            await parsedContentRepository.SaveParsedDocumentAsync(parsedDocument, cancellationToken);
            document.MarkCompleted(clock.UtcNow);
            await documentRepository.UpdateDocumentAsync(document, cancellationToken);

            return new ParseDocumentResult(
                DocumentResult.FromDocument(document),
                ParsedDocumentResult.FromParsedDocument(parsedDocument));
        }
        catch (Exception exception) when (document is not null && exception is DocumentParseException or DomainValidationException)
        {
            document.MarkFailed(exception.Message, clock.UtcNow);
            await documentRepository.UpdateDocumentAsync(document, CancellationToken.None);

            return new ParseDocumentResult(DocumentResult.FromDocument(document), ParsedDocument: null);
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
