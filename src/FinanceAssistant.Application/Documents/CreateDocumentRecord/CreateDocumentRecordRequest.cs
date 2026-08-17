namespace FinanceAssistant.Application.Documents.CreateDocumentRecord;

public sealed record CreateDocumentRecordRequest(
    string OriginalDisplayName,
    string VerifiedMediaType,
    Stream Content);
