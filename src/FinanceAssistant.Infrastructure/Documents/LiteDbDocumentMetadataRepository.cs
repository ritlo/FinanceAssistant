using FinanceAssistant.Application.Documents;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Documents;

public sealed class LiteDbDocumentMetadataRepository : IDocumentMetadataRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbDocumentMetadataRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task AddDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<DocumentMetadataDocument>(LiteDbCollectionNames.DocumentMetadata, BsonAutoId.Guid)
            .Insert(DocumentMetadataDocument.FromDocument(document));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UploadedDocument>> ListDocumentsAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var documents = database
            .GetCollection<DocumentMetadataDocument>(LiteDbCollectionNames.DocumentMetadata, BsonAutoId.Guid)
            .FindAll()
            .Where(document => document.ProfileId == profileId.Value)
            .Select(document => document.ToDocument())
            .ToArray();

        return Task.FromResult<IReadOnlyList<UploadedDocument>>(documents);
    }

    public Task<UploadedDocument?> GetDocumentAsync(
        LocalProfileId profileId,
        DocumentId documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<DocumentMetadataDocument>(LiteDbCollectionNames.DocumentMetadata, BsonAutoId.Guid)
            .FindById(documentId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<UploadedDocument?>(null);
        }

        return Task.FromResult<UploadedDocument?>(document.ToDocument());
    }

    public Task UpdateDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var existing = database
            .GetCollection<DocumentMetadataDocument>(LiteDbCollectionNames.DocumentMetadata, BsonAutoId.Guid)
            .FindById(document.Id.Value);

        if (existing is null || existing.ProfileId != document.ProfileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<DocumentMetadataDocument>(LiteDbCollectionNames.DocumentMetadata, BsonAutoId.Guid)
            .Update(document.Id.Value, DocumentMetadataDocument.FromDocument(document));

        return Task.CompletedTask;
    }
}
