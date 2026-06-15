using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Transactions;

public interface ITransactionRepository
{
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
