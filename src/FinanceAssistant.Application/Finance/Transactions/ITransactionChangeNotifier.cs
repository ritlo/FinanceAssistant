namespace FinanceAssistant.Application.Finance.Transactions;

public interface ITransactionChangeNotifier
{
    Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default);
}
