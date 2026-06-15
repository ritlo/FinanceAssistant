using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories.AddCategory;

public sealed record AddCategoryRequest(string Name, TransactionType TransactionType);
