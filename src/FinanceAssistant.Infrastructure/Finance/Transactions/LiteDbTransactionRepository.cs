using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

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
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .Insert(TransactionDocument.FromTransaction(transaction));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var categories = LoadCategories(database, profileId);
        var transactions = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .FindAll()
            .Where(doc => doc.ProfileId == profileId.Value)
            .Select(doc => ToTransaction(doc, categories))
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
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .FindById(transactionId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<Transaction?>(null);
        }

        var categories = LoadCategories(database, profileId);
        return Task.FromResult<Transaction?>(ToTransaction(document, categories));
    }

    public Task UpdateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .FindById(transaction.Id.Value);

        if (document is null || document.ProfileId != transaction.ProfileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
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
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .FindById(transactionId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<TransactionDocument>(LiteDbCollectionNames.Transactions, BsonAutoId.Guid)
            .Delete(transactionId.Value);

        return Task.CompletedTask;
    }

    private static IReadOnlyDictionary<Guid, Category> LoadCategories(
        LiteDatabase database,
        LocalProfileId profileId)
    {
        return database
            .GetCollection<CategoryDocument>(LiteDbCollectionNames.Categories, BsonAutoId.Guid)
            .FindAll()
            .Where(doc => doc.ProfileId == profileId.Value)
            .Select(doc => doc.ToCategory())
            .ToDictionary(category => category.Id.Value);
    }

    private static Transaction ToTransaction(
        TransactionDocument document,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (!categories.TryGetValue(document.CategoryId, out var category))
        {
            throw new InvalidOperationException("Transaction category was not found.");
        }

        return document.ToTransaction(category);
    }
}
