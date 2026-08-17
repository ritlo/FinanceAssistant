using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Finance.Transactions;

public interface ITransactionRepository
{
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetTransactionAsync(
        LocalProfileId profileId,
        TransactionId transactionId,
        CancellationToken cancellationToken = default);

    Task UpdateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    Task DeleteTransactionAsync(
        LocalProfileId profileId,
        TransactionId transactionId,
        CancellationToken cancellationToken = default);
}
