using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories.ResolveCategory;

public sealed record ResolveCategoryRequest(string Description, TransactionType TransactionType);
