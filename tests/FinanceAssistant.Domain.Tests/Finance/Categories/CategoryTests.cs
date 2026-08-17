using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Finance.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void CreateRequiresName()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Category.Create(LocalProfileId.New(), "  \t ", TransactionType.Expense));

        Assert.Equal("Category name is required.", exception.Message);
    }

    [Fact]
    public void CreateNormalizesNameWhitespaceAndCompatibilityType()
    {
        var profileId = LocalProfileId.New();

        var category = Category.Create(profileId, "  Food\tand\nDining  ", TransactionType.Expense);

        Assert.Equal(profileId, category.ProfileId);
        Assert.Equal("Food and Dining", category.Name);
        Assert.Equal(TransactionType.Expense, category.TransactionType);
    }

    [Fact]
    public void HasSameNameUsesOrdinalCaseInsensitiveComparisonAfterNormalization()
    {
        var category = Category.Create(LocalProfileId.New(), "Groceries", TransactionType.Expense);

        Assert.True(category.HasSameName(" groceries "));
        Assert.False(category.HasSameName("Grocery"));
    }
}
