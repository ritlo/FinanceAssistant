using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Tests.Finance.Summaries;

public sealed class GetMonthlySummaryUseCaseTests
{
    [Fact]
    public async Task SummaryIncludesOnlyExpenseTransactions()
    {
        var context = TestContext.Create();
        context.AddTransaction(20m, TransactionType.Expense, new DateOnly(2026, 6, 1), "Groceries", context.Groceries);
        context.AddTransaction(500m, TransactionType.Income, new DateOnly(2026, 6, 1), "Salary", context.Salary);

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Equal(20m, result.ExpenseTotal);
        var category = Assert.Single(result.Categories);
        Assert.Equal(context.Groceries.Id.Value, category.CategoryId);
        Assert.Equal(20m, category.Total);
    }

    [Fact]
    public async Task SummaryIncludesFirstAndLastDayOfSelectedMonth()
    {
        var context = TestContext.Create();
        context.AddTransaction(10m, TransactionType.Expense, new DateOnly(2026, 6, 1), "First day", context.Groceries);
        context.AddTransaction(15m, TransactionType.Expense, new DateOnly(2026, 6, 30), "Last day", context.Groceries);

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Equal(25m, result.ExpenseTotal);
        Assert.Equal(25m, Assert.Single(result.Categories).Total);
    }

    [Fact]
    public async Task SummaryExcludesAdjacentMonths()
    {
        var context = TestContext.Create();
        context.AddTransaction(10m, TransactionType.Expense, new DateOnly(2026, 5, 31), "Previous month", context.Groceries);
        context.AddTransaction(20m, TransactionType.Expense, new DateOnly(2026, 6, 15), "Selected month", context.Groceries);
        context.AddTransaction(30m, TransactionType.Expense, new DateOnly(2026, 7, 1), "Next month", context.Groceries);

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Equal(20m, result.ExpenseTotal);
        Assert.Equal(20m, Assert.Single(result.Categories).Total);
    }

    [Fact]
    public async Task SummaryGroupsByCategoryAndTotals()
    {
        var context = TestContext.Create();
        context.AddTransaction(10m, TransactionType.Expense, new DateOnly(2026, 6, 5), "Groceries one", context.Groceries);
        context.AddTransaction(15m, TransactionType.Expense, new DateOnly(2026, 6, 6), "Groceries two", context.Groceries);
        context.AddTransaction(7m, TransactionType.Expense, new DateOnly(2026, 6, 7), "Dining", context.Dining);

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Equal(32m, result.ExpenseTotal);
        Assert.Equal(2, result.Categories.Count);
        Assert.Contains(result.Categories, c => c.CategoryId == context.Groceries.Id.Value && c.Total == 25m);
        Assert.Contains(result.Categories, c => c.CategoryId == context.Dining.Id.Value && c.Total == 7m);
    }

    [Fact]
    public async Task SummaryOrdersByTotalDescendingThenCategoryName()
    {
        var context = TestContext.Create();
        var auto = context.AddCategory("Auto", TransactionType.Expense);
        var books = context.AddCategory("Books", TransactionType.Expense);
        context.AddTransaction(20m, TransactionType.Expense, new DateOnly(2026, 6, 5), "Dining", context.Dining);
        context.AddTransaction(10m, TransactionType.Expense, new DateOnly(2026, 6, 5), "Books", books);
        context.AddTransaction(10m, TransactionType.Expense, new DateOnly(2026, 6, 5), "Auto", auto);

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Collection(
            result.Categories,
            category => Assert.Equal(context.Dining.Name, category.CategoryName),
            category => Assert.Equal(auto.Name, category.CategoryName),
            category => Assert.Equal(books.Name, category.CategoryName));
    }

    [Fact]
    public async Task SummaryReturnsEmptyResultWhenNoRecordsMatch()
    {
        var context = TestContext.Create();

        var result = await context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(2026, 6));

        Assert.Equal(2026, result.Year);
        Assert.Equal(6, result.Month);
        Assert.Equal(0m, result.ExpenseTotal);
        Assert.Empty(result.Categories);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(10000, 6)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task SummaryRejectsInvalidMonthOrYear(int year, int month)
    {
        var context = TestContext.Create();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => context.UseCase.ExecuteAsync(new GetMonthlySummaryRequest(year, month)));

        Assert.Equal("Summary month is invalid.", exception.Message);
    }

    private sealed class TestContext
    {
        private TestContext(LocalProfileId profileId)
        {
            ProfileId = profileId;
            Categories = new FakeCategoryRepository();
            Transactions = new FakeTransactionRepository();
            Groceries = AddCategory("Groceries", TransactionType.Expense);
            Dining = AddCategory("Dining", TransactionType.Expense);
            Salary = AddCategory("Salary", TransactionType.Income);
            UseCase = new GetMonthlySummaryUseCase(
                new FixedCurrentProfileProvider(profileId),
                Transactions,
                Categories);
        }

        public LocalProfileId ProfileId { get; }

        public FakeCategoryRepository Categories { get; }

        public FakeTransactionRepository Transactions { get; }

        public Category Groceries { get; }

        public Category Dining { get; }

        public Category Salary { get; }

        public GetMonthlySummaryUseCase UseCase { get; }

        public static TestContext Create() => new(LocalProfileId.New());

        public Category AddCategory(string name, TransactionType transactionType)
        {
            var category = Category.Create(ProfileId, name, transactionType);
            Categories.AddExisting(category);
            return category;
        }

        public void AddTransaction(
            decimal amount,
            TransactionType type,
            DateOnly date,
            string description,
            Category category)
        {
            Transactions.AddExisting(Transaction.Create(
                ProfileId,
                Money.Create(amount),
                type,
                date,
                description,
                category));
        }
    }

    private sealed class FixedCurrentProfileProvider : ICurrentProfileProvider
    {
        private readonly LocalProfileId profileId;

        public FixedCurrentProfileProvider(LocalProfileId profileId)
        {
            this.profileId = profileId;
        }

        public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(profileId);
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> categories = [];

        public void AddExisting(Category category)
        {
            categories.Add(category);
        }

        public Task<IReadOnlyList<Category>> ListCategoriesAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(
                categories.Where(category => category.ProfileId == profileId).ToArray());
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CategorizationRule>> ListCategorizationRulesAsync(
            LocalProfileId profileId,
            TransactionType transactionType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategorizationRule>>([]);
        }

        public Task AddCategorizationRuleAsync(
            CategorizationRule rule,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly List<Transaction> transactions = [];

        public void AddExisting(Transaction transaction)
        {
            transactions.Add(transaction);
        }

        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(
                transactions.Where(transaction => transaction.ProfileId == profileId).ToArray());
        }

        public Task<Transaction?> GetTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Transaction?>(null);
        }

        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
