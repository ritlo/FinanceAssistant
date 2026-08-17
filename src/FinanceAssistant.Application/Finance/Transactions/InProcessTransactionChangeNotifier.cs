namespace FinanceAssistant.Application.Finance.Transactions;

public sealed class InProcessTransactionChangeNotifier : ITransactionChangeNotifier
{
    public event Func<CancellationToken, Task>? TransactionChanged;

    public async Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default)
    {
        if (TransactionChanged is null)
        {
            return;
        }

        foreach (var handler in TransactionChanged.GetInvocationList().Cast<Func<CancellationToken, Task>>())
        {
            await handler(cancellationToken);
        }
    }
}
