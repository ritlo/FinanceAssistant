using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories.ListCategorizationRules;

public sealed record ListCategorizationRulesRequest(TransactionType TransactionType);
