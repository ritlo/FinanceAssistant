using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Finance.Transactions.GetTransactions;

public sealed class GetTransactionsUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICategoryRepository categoryRepository;

    public GetTransactionsUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.transactionRepository = transactionRepository;
        this.categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<TransactionResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var transactions = await transactionRepository.ListTransactionsAsync(profileId, cancellationToken);
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);

        var categoryMap = categories.ToDictionary(c => c.Id);

        var results = transactions
            .Select(transaction =>
            {
                if (!categoryMap.TryGetValue(transaction.CategoryId, out var category))
                {
                    throw new InvalidOperationException("Transaction category was not found.");
                }

                return TransactionResult.FromTransaction(transaction, category);
            })
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Description, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return results;
    }
}
