using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;

namespace FinanceAssistant.Infrastructure.Finance.Transactions;

public sealed class LiteDbTransactionRepository : ITransactionRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbTransactionRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .Insert(TransactionDocument.FromTransaction(transaction));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var transactions = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .FindAll()
            .Where(doc => doc.ProfileId == profileId.Value)
            .Select(doc => doc.ToTransaction())
            .ToArray();

        return Task.FromResult<IReadOnlyList<Transaction>>(transactions);
    }

    public Task<Transaction?> GetTransactionAsync(
        LocalProfileId profileId,
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .FindById(transactionId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<Transaction?>(null);
        }

        return Task.FromResult<Transaction?>(document.ToTransaction());
    }

    public Task UpdateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .FindById(transaction.Id.Value);

        ArgumentNullException.ThrowIfNull(document, nameof(document));

        database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .Update(document.Id, TransactionDocument.FromTransaction(transaction));

        return Task.CompletedTask;
    }

    public Task DeleteTransactionAsync(
        LocalProfileId profileId,
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .FindById(transactionId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, LiteDB.BsonAutoId.Guid)
            .Delete(transactionId.Value);

        return Task.CompletedTask;
    }
}
