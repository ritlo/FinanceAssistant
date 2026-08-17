using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ListCategories;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Web.Finance.Transactions;

namespace FinanceAssistant.Web.Tests.Finance.Transactions;

public sealed class TransactionDashboardStateTests
{
    [Fact]
    public async Task InitializeLoadsTransactionsAndMonthlySummary()
    {
        var fixture = new Fixture();
        fixture.Transactions.Add(Transaction.Create(
            fixture.ProfileId,
            Money.Create(25m),
            TransactionType.Expense,
            new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 2),
            "Lunch",
            fixture.Category));

        await fixture.State.InitializeAsync();

        Assert.Single(fixture.State.Categories);
        Assert.Single(fixture.State.Transactions);
        Assert.NotNull(fixture.State.Summary);
        Assert.Equal(25m, fixture.State.Summary.ExpenseTotal);
    }

    [Fact]
    public async Task NotifierReloadsStateAndRaisesLocalStateChanged()
    {
        var fixture = new Fixture();
        var stateChangedCount = 0;
        fixture.State.StateChanged += () =>
        {
            stateChangedCount++;
            return Task.CompletedTask;
        };
        await fixture.State.InitializeAsync();
        fixture.Transactions.Add(Transaction.Create(
            fixture.ProfileId,
            Money.Create(10m),
            TransactionType.Expense,
            new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 3),
            "Coffee",
            fixture.Category));

        await fixture.Notifier.PublishTransactionChangedAsync();

        Assert.Equal(1, stateChangedCount);
        Assert.Single(fixture.State.Transactions);
        Assert.Equal(10m, fixture.State.Summary!.ExpenseTotal);
    }

    [Fact]
    public async Task DisposeUnsubscribesFromNotifier()
    {
        var fixture = new Fixture();
        var stateChangedCount = 0;
        fixture.State.StateChanged += () =>
        {
            stateChangedCount++;
            return Task.CompletedTask;
        };
        await fixture.State.InitializeAsync();
        fixture.State.Dispose();
        fixture.Transactions.Add(Transaction.Create(
            fixture.ProfileId,
            Money.Create(10m),
            TransactionType.Expense,
            new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 3),
            "Coffee",
            fixture.Category));

        await fixture.Notifier.PublishTransactionChangedAsync();

        Assert.Equal(0, stateChangedCount);
        Assert.Empty(fixture.State.Transactions);
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            CurrentProfileProvider = new FixedCurrentProfileProvider(ProfileId);
            Category = Category.Create(ProfileId, "Other", TransactionType.Expense);
            Categories.Add(Category);
            Notifier = new InProcessTransactionChangeNotifier();
            State = new TransactionDashboardState(
                new ListCategoriesUseCase(CurrentProfileProvider, Categories),
                new GetTransactionsUseCase(CurrentProfileProvider, Transactions, Categories),
                new GetMonthlySummaryUseCase(CurrentProfileProvider, Transactions, Categories),
                Notifier);
        }

        public LocalProfileId ProfileId { get; } = LocalProfileId.New();
        public FixedCurrentProfileProvider CurrentProfileProvider { get; }
        public FakeCategoryRepository Categories { get; } = new();
        public FakeTransactionRepository Transactions { get; } = new();
        public InProcessTransactionChangeNotifier Notifier { get; }
        public TransactionDashboardState State { get; }
        public Category Category { get; }
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

        public void Add(Category category)
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

        public Task AddCategorizationRuleAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly List<Transaction> transactions = [];

        public void Add(Transaction transaction)
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
            return Task.FromResult<Transaction?>(
                transactions.SingleOrDefault(transaction => transaction.ProfileId == profileId && transaction.Id == transactionId));
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
