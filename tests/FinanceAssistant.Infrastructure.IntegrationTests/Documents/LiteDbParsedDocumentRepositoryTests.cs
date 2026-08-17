using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Documents;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Documents;

[Collection("Sequential")]
public sealed class LiteDbParsedDocumentRepositoryTests
{
    [Fact]
    public async Task SaveAndGetParsedDocumentPersistsExtractedText()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbParsedDocumentRepository(options);
        var parsed = ParsedDocument.Create(
            DocumentId.New(),
            profileId,
            DocumentMediaTypes.PlainText,
            "untrusted extracted text",
            null,
            new DateTimeOffset(2026, 6, 16, 14, 0, 0, TimeSpan.Zero));

        await repository.SaveParsedDocumentAsync(parsed);

        var persisted = await repository.GetParsedDocumentAsync(profileId, parsed.DocumentId);
        Assert.NotNull(persisted);
        Assert.Equal("untrusted extracted text", persisted.UntrustedExtractedText);
        Assert.Equal(parsed.ParsedAt, persisted.ParsedAt);
    }

    [Fact]
    public async Task GetParsedDocumentPreventsCrossProfileAccess()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var repository = new LiteDbParsedDocumentRepository(options);
        var parsed = ParsedDocument.Create(
            DocumentId.New(),
            otherProfileId,
            DocumentMediaTypes.PlainText,
            "other",
            null,
            DateTimeOffset.UtcNow);

        await repository.SaveParsedDocumentAsync(parsed);

        var result = await repository.GetParsedDocumentAsync(profileId, parsed.DocumentId);
        Assert.Null(result);
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
