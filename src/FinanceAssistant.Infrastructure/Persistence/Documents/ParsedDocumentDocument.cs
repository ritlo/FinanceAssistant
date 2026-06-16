using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class ParsedDocumentDocument
{
    [BsonId]
    public Guid DocumentId { get; set; }

    public Guid ProfileId { get; set; }

    public string VerifiedMediaType { get; set; } = string.Empty;

    public string UntrustedExtractedText { get; set; } = string.Empty;

    public int? PdfPageCount { get; set; }

    public DateTimeOffset ParsedAt { get; set; }

    public static ParsedDocumentDocument FromParsedDocument(ParsedDocument parsedDocument)
    {
        return new ParsedDocumentDocument
        {
            DocumentId = parsedDocument.DocumentId.Value,
            ProfileId = parsedDocument.ProfileId.Value,
            VerifiedMediaType = parsedDocument.VerifiedMediaType,
            UntrustedExtractedText = parsedDocument.UntrustedExtractedText,
            PdfPageCount = parsedDocument.PdfPageCount,
            ParsedAt = parsedDocument.ParsedAt,
        };
    }

    public ParsedDocument ToParsedDocument()
    {
        return ParsedDocument.Create(
            new DocumentId(DocumentId),
            new LocalProfileId(ProfileId),
            VerifiedMediaType,
            UntrustedExtractedText,
            PdfPageCount,
            ParsedAt);
    }
}
