using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Domain.Finance.Transactions;
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
}
