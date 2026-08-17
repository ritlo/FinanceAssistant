using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories.AddCategorizationRule;

public sealed record AddCategorizationRuleRequest(
    string Keyword,
    TransactionType TransactionType,
    Guid CategoryId,
    int Order,
    bool IsActive = true);
