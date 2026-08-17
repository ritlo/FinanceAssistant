using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Domain.Finance.Categories;

public static class CategoryDefaults
{
    private static readonly IReadOnlyList<DefaultCategory> Defaults =
    [
        new("Salary", TransactionType.Income),
        new("Other", TransactionType.Income),
        new("Groceries", TransactionType.Expense),
        new("Rent", TransactionType.Expense),
        new("Utilities", TransactionType.Expense),
        new("Transport", TransactionType.Expense),
        new("Dining", TransactionType.Expense),
        new("Entertainment", TransactionType.Expense),
        new("Healthcare", TransactionType.Expense),
        new("Shopping", TransactionType.Expense),
        new("Services", TransactionType.Expense),
        new("Other", TransactionType.Expense),
    ];

    public static IReadOnlyList<DefaultCategory> All => Defaults;
}
