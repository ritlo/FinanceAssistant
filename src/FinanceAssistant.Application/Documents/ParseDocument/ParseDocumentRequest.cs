namespace FinanceAssistant.Application.Documents.ParseDocument;

public sealed record ParseDocumentRequest(
    string OriginalDisplayName,
    string DeclaredMediaType,
    Stream Content);
