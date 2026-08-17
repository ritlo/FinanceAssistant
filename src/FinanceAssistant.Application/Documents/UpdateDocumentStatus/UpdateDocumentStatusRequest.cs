using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents.UpdateDocumentStatus;

public sealed record UpdateDocumentStatusRequest(
    Guid Id,
    DocumentParseStatus ParseStatus,
    string? FailureReason = null);
