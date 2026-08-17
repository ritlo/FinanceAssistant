using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Transactions;

public sealed record TransactionResult(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    string Description,
    Guid CategoryId,
    string CategoryName)
{
    public static TransactionResult FromTransaction(
        Transaction transaction,
        Category category)
    {
        return new TransactionResult(
            transaction.Id.Value,
            transaction.Amount.Amount,
            transaction.Type,
            transaction.Date,
            transaction.Description,
            transaction.CategoryId.Value,
            category.Name);
    }
}

