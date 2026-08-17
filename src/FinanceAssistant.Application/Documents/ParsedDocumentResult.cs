using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents;

public sealed record ParsedDocumentResult(
    Guid DocumentId,
    string VerifiedMediaType,
    string UntrustedExtractedText,
    int? PdfPageCount,
    DateTimeOffset ParsedAt)
{
    public static ParsedDocumentResult FromParsedDocument(ParsedDocument parsedDocument)
    {
        return new ParsedDocumentResult(
            parsedDocument.DocumentId.Value,
            parsedDocument.VerifiedMediaType,
            parsedDocument.UntrustedExtractedText,
            parsedDocument.PdfPageCount,
            parsedDocument.ParsedAt);
    }
}
