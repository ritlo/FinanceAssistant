using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Domain.Finance.Categories;

public sealed record DefaultCategory(string Name, TransactionType TransactionType);
