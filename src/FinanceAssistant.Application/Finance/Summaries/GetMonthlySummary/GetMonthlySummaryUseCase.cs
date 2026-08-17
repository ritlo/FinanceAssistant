using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;

public sealed class GetMonthlySummaryUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ITransactionRepository transactionRepository;
    private readonly ICategoryRepository categoryRepository;

    public GetMonthlySummaryUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.transactionRepository = transactionRepository;
        this.categoryRepository = categoryRepository;
    }

    public async Task<GetMonthlySummaryResult> ExecuteAsync(
        GetMonthlySummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var transactions = await transactionRepository.ListTransactionsAsync(profileId, cancellationToken);
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);
        var categoryMap = categories.ToDictionary(category => category.Id);
        var firstDay = new DateOnly(request.Year, request.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var summaryCategories = transactions
            .Where(transaction =>
                transaction.Type == TransactionType.Expense &&
                transaction.Date >= firstDay &&
                transaction.Date <= lastDay)
            .GroupBy(transaction => transaction.CategoryId)
            .Select(group =>
            {
                if (!categoryMap.TryGetValue(group.Key, out var category))
                {
                    throw new InvalidOperationException("Transaction category was not found.");
                }

                return new MonthlySummaryCategoryResult(
                    category.Id.Value,
                    category.Name,
                    group.Sum(transaction => transaction.Amount.Amount));
            })
            .OrderByDescending(category => category.Total)
            .ThenBy(category => category.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GetMonthlySummaryResult(
            request.Year,
            request.Month,
            summaryCategories.Sum(category => category.Total),
            summaryCategories);
    }

    private static void ValidateRequest(GetMonthlySummaryRequest request)
    {
        if (request.Year is < 1 or > 9999 || request.Month is < 1 or > 12)
        {
            throw new DomainValidationException("Summary month is invalid.");
        }
    }
}
