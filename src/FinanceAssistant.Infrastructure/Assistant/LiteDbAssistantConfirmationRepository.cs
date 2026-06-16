using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Assistant;

public sealed class LiteDbAssistantConfirmationRepository : IAssistantConfirmationRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbAssistantConfirmationRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task AddAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        Collection(database).Insert(AssistantConfirmationDocument.FromRecord(record));

        return Task.CompletedTask;
    }

    public Task<AssistantConfirmationRecord?> GetByTokenAsync(
        LocalProfileId profileId,
        Guid token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = Collection(database).FindById(token);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<AssistantConfirmationRecord?>(null);
        }

        return Task.FromResult<AssistantConfirmationRecord?>(document.ToRecord());
    }

    public Task<AssistantConfirmationRecord?> GetByFingerprintAsync(
        LocalProfileId profileId,
        string operationFingerprint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = Collection(database)
            .FindAll()
            .Where(document => document.ProfileId == profileId.Value)
            .FirstOrDefault(document => document.OperationFingerprint == operationFingerprint);

        return Task.FromResult(document?.ToRecord());
    }

    public Task UpdateAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        Collection(database).Update(record.Token, AssistantConfirmationDocument.FromRecord(record));

        return Task.CompletedTask;
    }

    public Task<bool> TryClaimAsync(
        LocalProfileId profileId,
        Guid token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var collection = Collection(database);
        var document = collection.FindById(token);

        if (document is null
            || document.ProfileId != profileId.Value
            || document.Status != AssistantConfirmationStatus.Pending.ToString())
        {
            return Task.FromResult(false);
        }

        document.Status = AssistantConfirmationStatus.Claimed.ToString();
        collection.Update(token, document);

        return Task.FromResult(true);
    }

    private static ILiteCollection<AssistantConfirmationDocument> Collection(LiteDatabase database)
    {
        return database.GetCollection<AssistantConfirmationDocument>(
            LiteDbCollectionNames.AssistantConfirmations,
            BsonAutoId.Guid);
    }
}
