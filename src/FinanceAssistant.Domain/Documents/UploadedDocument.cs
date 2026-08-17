using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Documents;

public sealed class UploadedDocument
{
    public const long MaximumByteLength = 10 * 1024 * 1024;

    private UploadedDocument(
        DocumentId id,
        LocalProfileId profileId,
        string originalDisplayName,
        string verifiedMediaType,
        long byteLength,
        string sha256Hash,
        DocumentParseStatus parseStatus,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? failureReason)
    {
        Id = id;
        ProfileId = profileId;
        OriginalDisplayName = originalDisplayName;
        VerifiedMediaType = verifiedMediaType;
        ByteLength = byteLength;
        Sha256Hash = sha256Hash;
        ParseStatus = parseStatus;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        FailureReason = failureReason;
    }

    public DocumentId Id { get; }

    public LocalProfileId ProfileId { get; }

    public string OriginalDisplayName { get; }

    public string VerifiedMediaType { get; }

    public long ByteLength { get; }

    public string Sha256Hash { get; }

    public DocumentParseStatus ParseStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public static UploadedDocument Create(
        LocalProfileId profileId,
        string originalDisplayName,
        string verifiedMediaType,
        long byteLength,
        string sha256Hash,
        DateTimeOffset createdAt)
    {
        return Rehydrate(
            DocumentId.New(),
            profileId,
            originalDisplayName,
            verifiedMediaType,
            byteLength,
            sha256Hash,
            DocumentParseStatus.Pending,
            createdAt,
            createdAt,
            failureReason: null);
    }

    public static UploadedDocument Rehydrate(
        DocumentId id,
        LocalProfileId profileId,
        string originalDisplayName,
        string verifiedMediaType,
        long byteLength,
        string sha256Hash,
        DocumentParseStatus parseStatus,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? failureReason)
    {
        var normalizedName = RequiredText.Normalize(originalDisplayName, "Document display name");
        if (normalizedName.Contains('/') || normalizedName.Contains('\\'))
        {
            throw new DomainValidationException("Document display name must not contain path separators.");
        }

        if (!DocumentMediaTypes.IsSupported(verifiedMediaType))
        {
            throw new DomainValidationException("Document media type is not supported.");
        }

        ValidateByteLength(byteLength);
        ValidateSha256Hash(sha256Hash);
        ValidateFailureReason(parseStatus, failureReason);

        return new UploadedDocument(
            id,
            profileId,
            normalizedName,
            verifiedMediaType,
            byteLength,
            sha256Hash,
            parseStatus,
            createdAt,
            updatedAt,
            NormalizeFailureReason(parseStatus, failureReason));
    }

    public void MarkProcessing(DateTimeOffset updatedAt)
    {
        ParseStatus = DocumentParseStatus.Processing;
        UpdatedAt = updatedAt;
        FailureReason = null;
    }

    public void MarkCompleted(DateTimeOffset updatedAt)
    {
        ParseStatus = DocumentParseStatus.Completed;
        UpdatedAt = updatedAt;
        FailureReason = null;
    }

    public void MarkFailed(string failureReason, DateTimeOffset updatedAt)
    {
        ParseStatus = DocumentParseStatus.Failed;
        UpdatedAt = updatedAt;
        FailureReason = RequiredText.Normalize(failureReason, "Document failure reason");
    }

    private static void ValidateByteLength(long byteLength)
    {
        if (byteLength <= 0)
        {
            throw new DomainValidationException("Document byte length is required.");
        }

        if (byteLength > MaximumByteLength)
        {
            throw new DomainValidationException("Document exceeds the 10 MiB size limit.");
        }
    }

    private static void ValidateSha256Hash(string sha256Hash)
    {
        if (sha256Hash.Length != 64 || sha256Hash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new DomainValidationException("Document SHA-256 hash is required.");
        }
    }

    private static void ValidateFailureReason(DocumentParseStatus parseStatus, string? failureReason)
    {
        if (parseStatus == DocumentParseStatus.Failed && string.IsNullOrWhiteSpace(failureReason))
        {
            throw new DomainValidationException("Document failure reason is required.");
        }
    }

    private static string? NormalizeFailureReason(DocumentParseStatus parseStatus, string? failureReason)
    {
        if (parseStatus != DocumentParseStatus.Failed || failureReason is null)
        {
            return null;
        }

        return RequiredText.Normalize(failureReason, "Document failure reason");
    }
}
