using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Domain.Tests.Finance.Categories;

public sealed class CategoryDefaultsTests
{
    [Fact]
    public void AllIncludesTypeCompatibleFallbacksAndSalary()
    {
        Assert.Contains(CategoryDefaults.All, category =>
            category.Name == "Salary" && category.TransactionType == TransactionType.Income);
        Assert.Contains(CategoryDefaults.All, category =>
            category.Name == "Other" && category.TransactionType == TransactionType.Income);
        Assert.Contains(CategoryDefaults.All, category =>
            category.Name == "Other" && category.TransactionType == TransactionType.Expense);
    }

    [Fact]
    public void AllDoesNotRepeatNameWithinTransactionType()
    {
        var duplicate = CategoryDefaults.All
            .GroupBy(category => (category.TransactionType, Name: category.Name.ToUpperInvariant()))
            .FirstOrDefault(group => group.Count() > 1);

        Assert.Null(duplicate);
    }
}
