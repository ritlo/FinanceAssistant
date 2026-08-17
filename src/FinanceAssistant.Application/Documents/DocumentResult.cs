using FinanceAssistant.Domain.Documents;

namespace FinanceAssistant.Application.Documents;

public sealed record DocumentResult(
    Guid Id,
    string OriginalDisplayName,
    string VerifiedMediaType,
    long ByteLength,
    string Sha256Hash,
    DocumentParseStatus ParseStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? FailureReason)
{
    public static DocumentResult FromDocument(UploadedDocument document)
    {
        return new DocumentResult(
            document.Id.Value,
            document.OriginalDisplayName,
            document.VerifiedMediaType,
            document.ByteLength,
            document.Sha256Hash,
            document.ParseStatus,
            document.CreatedAt,
            document.UpdatedAt,
            document.FailureReason);
    }
}
