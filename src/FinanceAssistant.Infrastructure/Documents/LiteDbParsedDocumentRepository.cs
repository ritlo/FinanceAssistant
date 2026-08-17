using FinanceAssistant.Application.Documents;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Documents;

public sealed class LiteDbParsedDocumentRepository : IDocumentParsedContentRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbParsedDocumentRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task SaveParsedDocumentAsync(ParsedDocument parsedDocument, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<ParsedDocumentDocument>(LiteDbCollectionNames.ParsedDocuments, BsonAutoId.Guid)
            .Upsert(parsedDocument.DocumentId.Value, ParsedDocumentDocument.FromParsedDocument(parsedDocument));

        return Task.CompletedTask;
    }

    public Task<ParsedDocument?> GetParsedDocumentAsync(
        LocalProfileId profileId,
        DocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<ParsedDocumentDocument>(LiteDbCollectionNames.ParsedDocuments, BsonAutoId.Guid)
            .FindById(documentId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<ParsedDocument?>(null);
        }

        return Task.FromResult<ParsedDocument?>(document.ToParsedDocument());
    }
}
