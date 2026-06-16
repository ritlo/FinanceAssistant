using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ResolveCategory;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Tests.Finance.Transactions;

public sealed class TransactionMaintenanceUseCaseTests
{
    [Fact]
    public async Task ListTransactionsSortsNewestFirstAndJoinsCategoryNames()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var salary = categories.AddExisting(Category.Create(profileId, "Salary", TransactionType.Income));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository();
        var older = Transaction.Create(
            profileId,
            Money.Create(50m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Older expense",
            groceries);
        transactions.Added.Add(older);

        var newer = Transaction.Create(
            profileId,
            Money.Create(5000m),
            TransactionType.Income,
            new DateOnly(2026, 6, 15),
            "Newer income",
            salary);
        transactions.Added.Add(newer);

        var useCase = new GetTransactionsUseCase(
            new FixedCurrentProfileProvider(profileId),
            transactions,
            categories);

        var results = await useCase.ExecuteAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(newer.Id.Value, results[0].Id);
        Assert.Equal("Newer income", results[0].Description);
        Assert.Equal(salary.Name, results[0].CategoryName);
        Assert.Equal(older.Id.Value, results[1].Id);
        Assert.Equal("Older expense", results[1].Description);
        Assert.Equal(groceries.Name, results[1].CategoryName);
    }

    [Fact]
    public async Task ListTransactionsOnlyUsesCurrentProfileData()
    {
        var currentProfileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();

        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(currentProfileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(currentProfileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository();
        var currentTransaction = Transaction.Create(
            currentProfileId,
            Money.Create(20m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Current profile transaction",
            groceries);
        transactions.Added.Add(currentTransaction);

        var otherTransaction = Transaction.Create(
            otherProfileId,
            Money.Create(99m),
            TransactionType.Income,
            new DateOnly(2026, 6, 15),
            "Other profile transaction",
            Category.Create(otherProfileId, "Salary", TransactionType.Income));
        transactions.Added.Add(otherTransaction);

        var useCase = new GetTransactionsUseCase(
            new FixedCurrentProfileProvider(currentProfileId),
            transactions,
            categories);

        var results = await useCase.ExecuteAsync();

        Assert.Single(results);
        Assert.Equal(currentTransaction.Id.Value, results[0].Id);
    }

    [Fact]
    public async Task UpdateWithExplicitCategoryChangesAllFields()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var dining = categories.AddExisting(Category.Create(profileId, "Dining", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository();
        var transaction = Transaction.Create(
            profileId,
            Money.Create(10m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Original description",
            groceries);
        transactions.Added.Add(transaction);

        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new UpdateTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            notifier);

        var result = await useCase.ExecuteAsync(new UpdateTransactionRequest(
            transaction.Id.Value,
            25.50m,
            TransactionType.Expense,
            new DateOnly(2026, 7, 10),
            "Updated description",
            dining.Id.Value));

        Assert.Equal(25.50m, result.Amount);
        Assert.Equal(TransactionType.Expense, result.Type);
        Assert.Equal(new DateOnly(2026, 7, 10), result.Date);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(dining.Id.Value, result.CategoryId);
        Assert.Equal(dining.Name, result.CategoryName);

        var updatedTransaction = transactions.Updated.Single();
        Assert.Equal(25.50m, updatedTransaction.Amount.Amount);
        Assert.Equal(TransactionType.Expense, updatedTransaction.Type);
        Assert.Equal(new DateOnly(2026, 7, 10), updatedTransaction.Date);
        Assert.Equal("Updated description", updatedTransaction.Description);
        Assert.Equal(dining.Id, updatedTransaction.CategoryId);

        Assert.Equal(1, notifier.PublishCount);
    }

    [Fact]
    public async Task UpdateWithNoCategoryAutoResolvesByRule()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var services = categories.AddExisting(Category.Create(profileId, "Services", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        categories.AddExisting(CategorizationRule.Create(profileId, "tech", TransactionType.Expense, services, 10));
        categories.AddExisting(CategorizationRule.Create(profileId, "tech", TransactionType.Expense, groceries, 20));

        var transactions = new FakeTransactionRepository();
        var transaction = Transaction.Create(
            profileId,
            Money.Create(100m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Old description",
            groceries);
        transactions.Added.Add(transaction);

        var useCase = new UpdateTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            new RecordingTransactionChangeNotifier());

        var result = await useCase.ExecuteAsync(new UpdateTransactionRequest(
            transaction.Id.Value,
            100m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "New TECH purchase",
            null));

        Assert.Equal(services.Id.Value, result.CategoryId);
        Assert.Equal(services.Name, result.CategoryName);
    }

    [Fact]
    public async Task UpdateThrowsWhenTransactionNotFound()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository();
        var useCase = new UpdateTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            new RecordingTransactionChangeNotifier());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new UpdateTransactionRequest(
                Guid.NewGuid(),
                10m,
                TransactionType.Expense,
                new DateOnly(2026, 6, 1),
                "Description",
                null)));

        Assert.Equal("Transaction was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdatePublishesNotificationOnlyAfterPersistenceSucceeds()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository { ThrowOnUpdate = true };
        var transaction = Transaction.Create(
            profileId,
            Money.Create(10m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Description",
            groceries);
        transactions.Added.Add(transaction);

        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new UpdateTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            categories,
            transactions,
            notifier);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(new UpdateTransactionRequest(
                transaction.Id.Value,
                20m,
                TransactionType.Expense,
                new DateOnly(2026, 6, 1),
                "Updated",
                null)));

        Assert.Equal(0, notifier.PublishCount);
    }

    [Fact]
    public async Task DeleteRemovesOnlyCurrentProfileTransaction()
    {
        var currentProfileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();

        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(currentProfileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(currentProfileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository();
        var currentTransaction = Transaction.Create(
            currentProfileId,
            Money.Create(30m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Current profile",
            groceries);
        transactions.Added.Add(currentTransaction);

        var otherTransaction = Transaction.Create(
            otherProfileId,
            Money.Create(99m),
            TransactionType.Income,
            new DateOnly(2026, 6, 15),
            "Other profile",
            Category.Create(otherProfileId, "Salary", TransactionType.Income));
        transactions.Added.Add(otherTransaction);

        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new DeleteTransactionUseCase(
            new FixedCurrentProfileProvider(currentProfileId),
            transactions,
            notifier);

        await useCase.ExecuteAsync(new DeleteTransactionRequest(currentTransaction.Id.Value));

        var currentTransactions = await transactions.ListTransactionsAsync(currentProfileId);
        Assert.Empty(currentTransactions);

        var otherTransactions = await transactions.ListTransactionsAsync(otherProfileId);
        Assert.Single(otherTransactions);
        Assert.Equal(otherTransaction.Id.Value, otherTransactions[0].Id.Value);

        Assert.Equal(1, notifier.PublishCount);
    }

    [Fact]
    public async Task DeleteThrowsWhenTransactionNotFound()
    {
        var profileId = LocalProfileId.New();
        var transactions = new FakeTransactionRepository();
        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new DeleteTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            transactions,
            notifier);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new DeleteTransactionRequest(Guid.NewGuid())));

        Assert.Equal("Transaction was not found.", exception.Message);
        Assert.Equal(0, notifier.PublishCount);
    }

    [Fact]
    public async Task DeletePublishesNotificationOnlyAfterPersistenceSucceeds()
    {
        var profileId = LocalProfileId.New();
        var categories = new FakeCategoryRepository();
        var groceries = categories.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        categories.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));

        var transactions = new FakeTransactionRepository { ThrowOnDelete = true };
        var transaction = Transaction.Create(
            profileId,
            Money.Create(10m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Description",
            groceries);
        transactions.Added.Add(transaction);

        var notifier = new RecordingTransactionChangeNotifier();
        var useCase = new DeleteTransactionUseCase(
            new FixedCurrentProfileProvider(profileId),
            transactions,
            notifier);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(new DeleteTransactionRequest(transaction.Id.Value)));

        Assert.Equal(0, notifier.PublishCount);
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
        public bool ThrowOnUpdate { get; init; }
        public bool ThrowOnDelete { get; init; }

        public List<Transaction> Updated { get; } = [];

        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            Added.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(
                Added.Where(t => t.ProfileId == profileId).ToArray());
        }

        public Task<Transaction?> GetTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Transaction?>(
                Added.SingleOrDefault(t => t.Id == transactionId && t.ProfileId == profileId));
        }

        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            if (ThrowOnUpdate)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            var existing = Added.SingleOrDefault(t => t.Id == transaction.Id);
            if (existing is not null)
            {
                var index = Added.IndexOf(existing);
                Added[index] = transaction;
            }

            Updated.Add(transaction);
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            var existing = Added.SingleOrDefault(t => t.Id == transactionId && t.ProfileId == profileId);
            if (existing is not null)
            {
                Added.Remove(existing);
            }

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
