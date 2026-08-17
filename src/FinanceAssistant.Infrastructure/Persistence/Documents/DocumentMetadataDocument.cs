using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class DocumentMetadataDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string OriginalDisplayName { get; set; } = string.Empty;

    public string VerifiedMediaType { get; set; } = string.Empty;

    public long ByteLength { get; set; }

    public string Sha256Hash { get; set; } = string.Empty;

    public string ParseStatus { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? FailureReason { get; set; }

    public static DocumentMetadataDocument FromDocument(UploadedDocument document)
    {
        return new DocumentMetadataDocument
        {
            Id = document.Id.Value,
            ProfileId = document.ProfileId.Value,
            OriginalDisplayName = document.OriginalDisplayName,
            VerifiedMediaType = document.VerifiedMediaType,
            ByteLength = document.ByteLength,
            Sha256Hash = document.Sha256Hash,
            ParseStatus = document.ParseStatus.ToString(),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            FailureReason = document.FailureReason,
        };
    }

    public UploadedDocument ToDocument()
    {
        return UploadedDocument.Rehydrate(
            new DocumentId(Id),
            new LocalProfileId(ProfileId),
            OriginalDisplayName,
            VerifiedMediaType,
            ByteLength,
            Sha256Hash,
            Enum.Parse<DocumentParseStatus>(ParseStatus, ignoreCase: false),
            CreatedAt,
            UpdatedAt,
            FailureReason);
    }
}
