using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Documents;

public sealed class UploadedDocumentTests
{
    private const string ValidHash = "4d9673ada5d847cbc70bf22d574d99564badb38bdcb2662388fe96af5b2ca439";

    [Fact]
    public void CreateStoresMetadataAsPending()
    {
        var createdAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);

        var document = UploadedDocument.Create(
            LocalProfileId.New(),
            "statement.pdf",
            DocumentMediaTypes.Pdf,
            2048,
            ValidHash,
            createdAt);

        Assert.Equal("statement.pdf", document.OriginalDisplayName);
        Assert.Equal(DocumentMediaTypes.Pdf, document.VerifiedMediaType);
        Assert.Equal(2048, document.ByteLength);
        Assert.Equal(ValidHash, document.Sha256Hash);
        Assert.Equal(DocumentParseStatus.Pending, document.ParseStatus);
        Assert.Equal(createdAt, document.CreatedAt);
        Assert.Equal(createdAt, document.UpdatedAt);
        Assert.Null(document.FailureReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("application/json")]
    public void CreateRejectsUnsupportedMediaType(string mediaType)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => UploadedDocument.Create(
                LocalProfileId.New(),
                "statement.pdf",
                mediaType,
                2048,
                ValidHash,
                DateTimeOffset.UtcNow));

        Assert.Equal("Document media type is not supported.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(UploadedDocument.MaximumByteLength + 1)]
    public void CreateRejectsInvalidByteLength(long byteLength)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => UploadedDocument.Create(
                LocalProfileId.New(),
                "statement.pdf",
                DocumentMediaTypes.Pdf,
                byteLength,
                ValidHash,
                DateTimeOffset.UtcNow));

        Assert.True(
            exception.Message is "Document byte length is required." or "Document exceeds the 10 MiB size limit.");
    }

    [Fact]
    public void CreateRejectsPathLikeDisplayName()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => UploadedDocument.Create(
                LocalProfileId.New(),
                "../statement.pdf",
                DocumentMediaTypes.Pdf,
                2048,
                ValidHash,
                DateTimeOffset.UtcNow));

        Assert.Equal("Document display name must not contain path separators.", exception.Message);
    }

    [Fact]
    public void CreateRejectsInvalidHash()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => UploadedDocument.Create(
                LocalProfileId.New(),
                "statement.pdf",
                DocumentMediaTypes.Pdf,
                2048,
                "not-a-sha",
                DateTimeOffset.UtcNow));

        Assert.Equal("Document SHA-256 hash is required.", exception.Message);
    }

    [Fact]
    public void StatusTransitionsUpdateStatusTimestampAndFailureReason()
    {
        var document = UploadedDocument.Create(
            LocalProfileId.New(),
            "statement.pdf",
            DocumentMediaTypes.Pdf,
            2048,
            ValidHash,
            new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));

        document.MarkProcessing(new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero));
        Assert.Equal(DocumentParseStatus.Processing, document.ParseStatus);
        Assert.Null(document.FailureReason);

        document.MarkFailed(" Parser failed. ", new DateTimeOffset(2026, 6, 16, 12, 2, 0, TimeSpan.Zero));
        Assert.Equal(DocumentParseStatus.Failed, document.ParseStatus);
        Assert.Equal("Parser failed.", document.FailureReason);

        document.MarkCompleted(new DateTimeOffset(2026, 6, 16, 12, 3, 0, TimeSpan.Zero));
        Assert.Equal(DocumentParseStatus.Completed, document.ParseStatus);
        Assert.Null(document.FailureReason);
        Assert.Equal(new DateTimeOffset(2026, 6, 16, 12, 3, 0, TimeSpan.Zero), document.UpdatedAt);
    }
}
