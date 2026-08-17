namespace FinanceAssistant.Application.Documents.ParseDocument;

public sealed record ParseDocumentResult(
    DocumentResult Document,
    ParsedDocumentResult? ParsedDocument);
