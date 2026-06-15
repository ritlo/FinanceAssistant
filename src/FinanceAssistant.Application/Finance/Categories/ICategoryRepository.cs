using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Finance.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListCategoriesAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default);

    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorizationRule>> ListCategorizationRulesAsync(
        LocalProfileId profileId,
        TransactionType transactionType,
        CancellationToken cancellationToken = default);

    Task AddCategorizationRuleAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default);
}
