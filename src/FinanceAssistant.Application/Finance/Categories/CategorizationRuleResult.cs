using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories;

public sealed record CategorizationRuleResult(
    Guid Id,
    string Keyword,
    TransactionType TransactionType,
    Guid CategoryId,
    int Order,
    bool IsActive)
{
    public static CategorizationRuleResult FromRule(CategorizationRule rule)
    {
        return new CategorizationRuleResult(
            rule.Id.Value,
            rule.Keyword,
            rule.TransactionType,
            rule.CategoryId.Value,
            rule.Order,
            rule.IsActive);
    }
}
