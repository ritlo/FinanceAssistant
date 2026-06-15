using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Finance.Categories;

public sealed class CategorizationRuleTests
{
    [Fact]
    public void CreateRequiresKeyword()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => CategorizationRule.Create(profileId, " ", TransactionType.Expense, category, 1));

        Assert.Equal("Categorization keyword is required.", exception.Message);
    }

    [Fact]
    public void CreateRequiresPositiveOrder()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => CategorizationRule.Create(profileId, "market", TransactionType.Expense, category, 0));

        Assert.Equal("Categorization rule order must be positive.", exception.Message);
    }

    [Fact]
    public void CreateRequiresCategoryFromSameProfile()
    {
        var category = Category.Create(LocalProfileId.New(), "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => CategorizationRule.Create(
                LocalProfileId.New(),
                "market",
                TransactionType.Expense,
                category,
                1));

        Assert.Equal("Rule category must belong to the same local profile.", exception.Message);
    }

    [Fact]
    public void CreateRequiresTypeCompatibleCategory()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Salary", TransactionType.Income);

        var exception = Assert.Throws<DomainValidationException>(
            () => CategorizationRule.Create(profileId, "market", TransactionType.Expense, category, 1));

        Assert.Equal("Rule category must be compatible with transaction type.", exception.Message);
    }

    [Fact]
    public void MatchesUsesNormalizedCaseInsensitiveSubstringComparison()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);
        var rule = CategorizationRule.Create(profileId, "  cafe\u0301   Nero  ", TransactionType.Expense, category, 1);

        Assert.Equal("café Nero", rule.Keyword);
        Assert.True(rule.Matches("Morning spend at CAFÉ   NERO"));
        Assert.False(rule.Matches("Morning spend at another cafe"));
    }

    [Fact]
    public void MatchesIgnoresInactiveRules()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);
        var rule = CategorizationRule.Create(profileId, "market", TransactionType.Expense, category, 1, isActive: false);

        Assert.False(rule.Matches("Market purchase"));
    }
}
