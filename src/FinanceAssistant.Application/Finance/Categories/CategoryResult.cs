using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Categories;

public sealed record CategoryResult(Guid Id, string Name, TransactionType TransactionType)
{
    public static CategoryResult FromCategory(Category category)
    {
        return new CategoryResult(category.Id.Value, category.Name, category.TransactionType);
    }
}
