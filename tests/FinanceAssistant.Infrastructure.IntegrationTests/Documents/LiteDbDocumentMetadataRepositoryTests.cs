using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Documents;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Documents;

[Collection("Sequential")]
public sealed class LiteDbDocumentMetadataRepositoryTests
{
    private const string ValidHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    [Fact]
    public async Task AddListAndUpdatePersistDocumentMetadata()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbDocumentMetadataRepository(options);
        var document = UploadedDocument.Create(
            profileId,
            "statement.txt",
            DocumentMediaTypes.PlainText,
            5,
            ValidHash,
            new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));

        await repository.AddDocumentAsync(document);
        document.MarkFailed("Parser failed", new DateTimeOffset(2026, 6, 16, 12, 30, 0, TimeSpan.Zero));
        await repository.UpdateDocumentAsync(document);

        var persisted = Assert.Single(await repository.ListDocumentsAsync(profileId));
        Assert.Equal(document.Id, persisted.Id);
        Assert.Equal("statement.txt", persisted.OriginalDisplayName);
        Assert.Equal(DocumentMediaTypes.PlainText, persisted.VerifiedMediaType);
        Assert.Equal(5, persisted.ByteLength);
        Assert.Equal(ValidHash, persisted.Sha256Hash);
        Assert.Equal(DocumentParseStatus.Failed, persisted.ParseStatus);
        Assert.Equal("Parser failed", persisted.FailureReason);
    }

    [Fact]
    public async Task GetAndUpdatePreventCrossProfileAccess()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var repository = new LiteDbDocumentMetadataRepository(options);
        var other = UploadedDocument.Create(
            otherProfileId,
            "other.txt",
            DocumentMediaTypes.PlainText,
            5,
            ValidHash,
            DateTimeOffset.UtcNow);
        await repository.AddDocumentAsync(other);

        var crossProfileDocument = await repository.GetDocumentAsync(profileId, other.Id);
        other.MarkCompleted(DateTimeOffset.UtcNow);
        await repository.UpdateDocumentAsync(other);

        Assert.Null(crossProfileDocument);
        var persistedOther = await repository.GetDocumentAsync(otherProfileId, other.Id);
        Assert.Equal(DocumentParseStatus.Completed, persistedOther!.ParseStatus);
    }

    private static FinanceAssistantDataOptions CreateOptions(TemporaryDirectory directory)
    {
        return new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            DocumentTemporaryDirectoryPath = Path.Combine(directory.Path, "document-temp"),
            Currency = "USD",
        };
    }
}
