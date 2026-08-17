using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Documents;

public sealed class ParsedDocument
{
    public const int MaximumExtractedTextLength = 200_000;
    public const int MaximumPdfPageCount = 100;

    private ParsedDocument(
        DocumentId documentId,
        LocalProfileId profileId,
        string verifiedMediaType,
        string untrustedExtractedText,
        int? pdfPageCount,
        DateTimeOffset parsedAt)
    {
        DocumentId = documentId;
        ProfileId = profileId;
        VerifiedMediaType = verifiedMediaType;
        UntrustedExtractedText = untrustedExtractedText;
        PdfPageCount = pdfPageCount;
        ParsedAt = parsedAt;
    }

    public DocumentId DocumentId { get; }

    public LocalProfileId ProfileId { get; }

    public string VerifiedMediaType { get; }

    public string UntrustedExtractedText { get; }

    public int? PdfPageCount { get; }

    public DateTimeOffset ParsedAt { get; }

    public static ParsedDocument Create(
        DocumentId documentId,
        LocalProfileId profileId,
        string verifiedMediaType,
        string untrustedExtractedText,
        int? pdfPageCount,
        DateTimeOffset parsedAt)
    {
        if (!DocumentMediaTypes.IsSupported(verifiedMediaType))
        {
            throw new Common.DomainValidationException("Document media type is not supported.");
        }

        if (untrustedExtractedText.Length > MaximumExtractedTextLength)
        {
            throw new Common.DomainValidationException("Extracted document text exceeds the storage limit.");
        }

        if (verifiedMediaType == DocumentMediaTypes.Pdf && pdfPageCount is null)
        {
            throw new Common.DomainValidationException("PDF page count is required.");
        }

        if (pdfPageCount > MaximumPdfPageCount)
        {
            throw new Common.DomainValidationException("PDF document exceeds the 100-page limit.");
        }

        if (verifiedMediaType != DocumentMediaTypes.Pdf && pdfPageCount is not null)
        {
            throw new Common.DomainValidationException("Page count is only supported for PDF documents.");
        }

        return new ParsedDocument(
            documentId,
            profileId,
            verifiedMediaType,
            untrustedExtractedText,
            pdfPageCount,
            parsedAt);
    }
}
