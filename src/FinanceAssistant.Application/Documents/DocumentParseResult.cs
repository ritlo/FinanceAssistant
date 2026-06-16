namespace FinanceAssistant.Application.Documents;

public sealed record DocumentParseResult(
    string VerifiedMediaType,
    string UntrustedExtractedText,
    int? PdfPageCount);
