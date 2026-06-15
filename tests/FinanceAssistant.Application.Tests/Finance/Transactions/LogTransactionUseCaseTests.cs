using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Tests.Finance.Transactions;

public sealed class LogTransactionUseCaseTests
{
    [Fact]
    public async Task ExecuteUsesExplicitCategoryWhenProvided()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        var transactions = new FakeTransactionRepository();
        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new LogTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            notifier);

        var result = await useCase.ExecuteAsync(new LogTransactionRequest(
            12.34m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Corner shop",
            groceries.Id.Value));

        Assert.Equal(groceries.Id.Value, result.CategoryId);
        Assert.Equal(groceries.Id, transactions.Added.Single().CategoryId);
        Assert.Equal(1, notifier.PublishCount);
    }

    [Fact]
    public async Task ExecuteResolvesCategoryByFirstMatchingRule()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var services = categories.AddExisting(Category.Create(profileId, "Services", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        categories.AddExisting(CategorizationRule.Create(profileId, "market", TransactionType.Expense, groceries, 20));
        categories.AddExisting(CategorizationRule.Create(profileId, "market", TransactionType.Expense, services, 10));
        var transactions = new FakeTransactionRepository();
        var useCase = new LogTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            new RecordingTransactionChangeNotifier());

        var result = await useCase.ExecuteAsync(new LogTransactionRequest(
            9.99m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Corner MARKET"));

        Assert.Equal(services.Id.Value, result.CategoryId);
    }

    [Fact]
    public async Task ExecuteFallsBackToTypeCompatibleOther()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var expenseOther = categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Income));
        var useCase = new LogTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            new FakeTransactionRepository(),
            new RecordingTransactionChangeNotifier());

        var result = await useCase.ExecuteAsync(new LogTransactionRequest(
            20m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Unmatched description"));

        Assert.Equal(expenseOther.Id.Value, result.CategoryId);
    }

    [Fact]
    public async Task ExecutePublishesNotificationOnlyAfterPersistenceSucceeds()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        var transactions = new FakeTransactionRepository { ThrowOnAdd = true };
        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new LogTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            notifier);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(new LogTransactionRequest(
                20m,
                TransactionType.Expense,
                new DateOnly(2026, 6, 15),
                "Unmatched description")));

        Assert.Equal(0, notifier.PublishCount);
    }

    [Fact]
    public async Task ExecuteRejectsMissingExplicitCategory()
    {
        var profileId = LocalProfileId.New();
        var useCase = new LogTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            new FakeCategoryRepository(),
            new FakeTransactionRepository(),
            new RecordingTransactionChangeNotifier());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new LogTransactionRequest(
                20m,
                TransactionType.Expense,
                new DateOnly(2026, 6, 15),
                "Unmatched description",
                Guid.NewGuid())));

        Assert.Equal("Category must exist for the local profile.", exception.Message);
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
        private readonly List<CategorizationRule> rules = [];

        public Category AddExisting(Category category)
        {
            categories.Add(category);
            return category;
        }

        public CategorizationRule AddExisting(CategorizationRule rule)
        {
            rules.Add(rule);
            return rule;
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
            return Task.FromResult<IReadOnlyList<CategorizationRule>>(
                rules
                    .Where(rule => rule.ProfileId == profileId && rule.TransactionType == transactionType)
                    .ToArray());
        }

        public Task AddCategorizationRuleAsync(
            CategorizationRule rule,
            CancellationToken cancellationToken = default)
        {
            rules.Add(rule);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Added { get; } = [];

        public bool ThrowOnAdd { get; init; }

        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            Added.Add(transaction);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTransactionChangeNotifier : ITransactionChangeNotifier
    {
        public int PublishCount { get; private set; }

        public Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default)
        {
            PublishCount++;
            return Task.CompletedTask;
        }
    }
}
