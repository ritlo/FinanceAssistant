using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Finance.Transactions;

public sealed class TransactionTests
{
    [Fact]
    public void CreateRequiresDate()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => Transaction.Create(
                profileId,
                Money.Create(12.34m),
                TransactionType.Expense,
                default,
                "Milk",
                category));

        Assert.Equal("Transaction date is required.", exception.Message);
    }

    [Fact]
    public void CreateRequiresDescription()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => Transaction.Create(
                profileId,
                Money.Create(12.34m),
                TransactionType.Expense,
                new DateOnly(2026, 6, 15),
                "  ",
                category));

        Assert.Equal("Transaction description is required.", exception.Message);
    }

    [Fact]
    public void CreateStoresDateOnlyWithoutTimezoneConversion()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Salary", TransactionType.Income);
        var date = new DateOnly(2026, 6, 15);

        var transaction = Transaction.Create(
            profileId,
            Money.Create(1000m),
            TransactionType.Income,
            date,
            "Monthly salary",
            category);

        Assert.Equal(date, transaction.Date);
    }

    [Fact]
    public void CreateRejectsCategoryForDifferentProfile()
    {
        var category = Category.Create(LocalProfileId.New(), "Groceries", TransactionType.Expense);

        var exception = Assert.Throws<DomainValidationException>(
            () => Transaction.Create(
                LocalProfileId.New(),
                Money.Create(12.34m),
                TransactionType.Expense,
                new DateOnly(2026, 6, 15),
                "Milk",
                category));

        Assert.Equal("Category must belong to the same local profile.", exception.Message);
    }

    [Fact]
    public void CreateRejectsTypeIncompatibleCategory()
    {
        var profileId = LocalProfileId.New();
        var category = Category.Create(profileId, "Salary", TransactionType.Income);

        var exception = Assert.Throws<DomainValidationException>(
            () => Transaction.Create(
                profileId,
                Money.Create(12.34m),
                TransactionType.Expense,
                new DateOnly(2026, 6, 15),
                "Milk",
                category));

        Assert.Equal("Category must be compatible with transaction type.", exception.Message);
    }

    [Fact]
    public void UpdateAppliesValidatedValues()
    {
        var profileId = LocalProfileId.New();
        var groceries = Category.Create(profileId, "Groceries", TransactionType.Expense);
        var rent = Category.Create(profileId, "Rent", TransactionType.Expense);
        var transaction = Transaction.Create(
            profileId,
            Money.Create(12.34m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Milk",
            groceries);

        transaction.Update(
            Money.Create(1200m),
            TransactionType.Expense,
            new DateOnly(2026, 7, 1),
            " Apartment rent ",
            rent);

        Assert.Equal(1200m, transaction.Amount.Amount);
        Assert.Equal(new DateOnly(2026, 7, 1), transaction.Date);
        Assert.Equal("Apartment rent", transaction.Description);
        Assert.Equal(rent.Id, transaction.CategoryId);
    }
}
