using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;

public sealed class DeleteTransactionUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ITransactionRepository transactionRepository;
    private readonly ITransactionChangeNotifier transactionChangeNotifier;

    public DeleteTransactionUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ITransactionRepository transactionRepository,
        ITransactionChangeNotifier transactionChangeNotifier)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.transactionRepository = transactionRepository;
        this.transactionChangeNotifier = transactionChangeNotifier;
    }

    public async Task ExecuteAsync(
        DeleteTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var transaction = await transactionRepository.GetTransactionAsync(profileId, new TransactionId(request.Id), cancellationToken);

        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }

        await transactionRepository.DeleteTransactionAsync(profileId, transaction.Id, cancellationToken);
        await transactionChangeNotifier.PublishTransactionChangedAsync(cancellationToken);
    }
}
