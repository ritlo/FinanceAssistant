using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Finance.Transactions;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Finance.Transactions;

[Collection("Sequential")]
public sealed class LiteDbTransactionRepositoryTests
{
    [Fact]
    public async Task LogTransactionPersistsTransactionAndUsesSeededFallbackCategory()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var result = await useCase.ExecuteAsync(new LogTransactionRequest(
            42.10m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "No matching rule"));

        using var database = new LiteDatabase(options.DatabasePath);
        var document = database.GetCollection("transactions").FindById(result.Id);

        Assert.NotNull(document);
        Assert.Equal(profileId.Value, document["ProfileId"].AsGuid);
        Assert.Equal(42.10m, document["Amount"].AsDecimal);
        Assert.Equal("Expense", document["Type"].AsString);
        Assert.Equal("2026-06-15", document["Date"].AsString);
        Assert.Equal(result.CategoryId, document["CategoryId"].AsGuid);
    }

    [Fact]
    public async Task ListReturnsPersistedTransactionsForInitializedProfile()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var logResult = await useCase.ExecuteAsync(new LogTransactionRequest(
            18.75m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Persisted transaction"));

        var repository = new LiteDbTransactionRepository(options);

        var result = await repository.ListTransactionsAsync(profileId);

        var transaction = Assert.Single(result);
        Assert.Equal(logResult.Id, transaction.Id.Value);
        Assert.Equal(18.75m, transaction.Amount.Amount);
        Assert.Equal("Persisted transaction", transaction.Description);
    }

    [Fact]
    public async Task ListExcludesTransactionsForAnotherProfileInsertedDirectly()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var currentResult = await useCase.ExecuteAsync(new LogTransactionRequest(
            12.25m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Current profile transaction"));

        var otherProfileId = LocalProfileId.New();
        var otherCategoryId = Guid.NewGuid();
        using (var database = new LiteDatabase(options.DatabasePath))
        {
            database.GetCollection<CategoryDocument>("categories", BsonAutoId.Guid).Insert(new CategoryDocument
            {
                Id = otherCategoryId,
                ProfileId = otherProfileId.Value,
                Name = "Other income",
                TransactionType = TransactionType.Income.ToString(),
            });
            database.GetCollection<TransactionDocument>("transactions", BsonAutoId.Guid).Insert(new TransactionDocument
            {
                Id = Guid.NewGuid(),
                ProfileId = otherProfileId.Value,
                Amount = 99m,
                Type = TransactionType.Income.ToString(),
                Date = "2026-06-16",
                Description = "Other profile transaction",
                CategoryId = otherCategoryId,
            });
        }

        var repository = new LiteDbTransactionRepository(options);
        var result = await repository.ListTransactionsAsync(profileId);

        var transaction = Assert.Single(result);
        Assert.Equal(currentResult.Id, transaction.Id.Value);
    }

    [Fact]
    public async Task GetReturnsTransactionByProfileAndId()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var logResult = await useCase.ExecuteAsync(new LogTransactionRequest(
            25.50m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Test transaction"));

        var repository = new LiteDbTransactionRepository(options);
        var transaction = await repository.GetTransactionAsync(profileId, new TransactionId(logResult.Id));

        Assert.NotNull(transaction);
        Assert.Equal(logResult.Id, transaction.Id.Value);
        Assert.Equal(25.50m, transaction.Amount.Amount);
    }

    [Fact]
    public async Task GetReturnsNullForTransactionBelongingToAnotherProfile()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var otherCategoryId = Guid.NewGuid();
        var otherTransactionId = Guid.NewGuid();
        using (var database = new LiteDatabase(options.DatabasePath))
        {
            database.GetCollection<CategoryDocument>("categories", BsonAutoId.Guid).Insert(new CategoryDocument
            {
                Id = otherCategoryId,
                ProfileId = otherProfileId.Value,
                Name = "Other income",
                TransactionType = TransactionType.Income.ToString(),
            });
            database.GetCollection<TransactionDocument>("transactions", BsonAutoId.Guid).Insert(new TransactionDocument
            {
                Id = otherTransactionId,
                ProfileId = otherProfileId.Value,
                Amount = 99m,
                Type = TransactionType.Income.ToString(),
                Date = "2026-06-16",
                Description = "Other profile transaction",
                CategoryId = otherCategoryId,
            });
        }

        var repository = new LiteDbTransactionRepository(options);

        var transaction = await repository.GetTransactionAsync(profileId, new TransactionId(otherTransactionId));

        Assert.Null(transaction);
    }

    [Fact]
    public async Task GetThrowsControlledExceptionWhenPersistedCategoryIsMissing()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var transactionId = Guid.NewGuid();
        using (var database = new LiteDatabase(options.DatabasePath))
        {
            database.GetCollection<TransactionDocument>("transactions", BsonAutoId.Guid).Insert(new TransactionDocument
            {
                Id = transactionId,
                ProfileId = profileId.Value,
                Amount = 99m,
                Type = TransactionType.Expense.ToString(),
                Date = "2026-06-16",
                Description = "Missing category transaction",
                CategoryId = Guid.NewGuid(),
            });
        }

        var repository = new LiteDbTransactionRepository(options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.GetTransactionAsync(profileId, new TransactionId(transactionId)));

        Assert.Equal("Transaction category was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdatePersistsChangedFields()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var logResult = await useCase.ExecuteAsync(new LogTransactionRequest(
            10m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 1),
            "Original description"));

        var updateUseCase = new UpdateTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        await updateUseCase.ExecuteAsync(new UpdateTransactionRequest(
            logResult.Id,
            50m,
            TransactionType.Expense,
            new DateOnly(2026, 7, 15),
            "Updated description",
            null));

        var repository = new LiteDbTransactionRepository(options);
        var updated = await repository.GetTransactionAsync(profileId, new TransactionId(logResult.Id));

        Assert.NotNull(updated);
        Assert.Equal(50m, updated.Amount.Amount);
        Assert.Equal(new DateOnly(2026, 7, 15), updated.Date);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task UpdateDoesNothingWhenStoredTransactionBelongsToAnotherProfile()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var transactionId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        using (var database = new LiteDatabase(options.DatabasePath))
        {
            database.GetCollection<CategoryDocument>("categories", BsonAutoId.Guid).Insert(new CategoryDocument
            {
                Id = otherCategoryId,
                ProfileId = otherProfileId.Value,
                Name = "Other expense",
                TransactionType = TransactionType.Expense.ToString(),
            });
            database.GetCollection<TransactionDocument>("transactions", BsonAutoId.Guid).Insert(new TransactionDocument
            {
                Id = transactionId,
                ProfileId = otherProfileId.Value,
                Amount = 11m,
                Type = TransactionType.Expense.ToString(),
                Date = "2026-06-01",
                Description = "Original other profile",
                CategoryId = otherCategoryId,
            });
        }

        var currentCategory = GetFirstExpenseCategory(options, profileId);
        var attemptedUpdate = Transaction.Rehydrate(
            new TransactionId(transactionId),
            profileId,
            Money.Create(88m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 2),
            "Attempted overwrite",
            currentCategory);
        var repository = new LiteDbTransactionRepository(options);

        await repository.UpdateTransactionAsync(attemptedUpdate);

        using var assertionDatabase = new LiteDatabase(options.DatabasePath);
        var document = assertionDatabase.GetCollection("transactions").FindById(transactionId);
        Assert.NotNull(document);
        Assert.Equal(otherProfileId.Value, document["ProfileId"].AsGuid);
        Assert.Equal(11m, document["Amount"].AsDecimal);
        Assert.Equal("Original other profile", document["Description"].AsString);
    }

    [Fact]
    public async Task DeleteRemovesTheTransaction()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var useCase = new LogTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbCategoryRepository(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        var logResult = await useCase.ExecuteAsync(new LogTransactionRequest(
            30m,
            TransactionType.Expense,
            new DateOnly(2026, 6, 15),
            "Delete me"));

        var deleteUseCase = new DeleteTransactionUseCase(
            new LiteDbCurrentProfileProvider(options),
            new LiteDbTransactionRepository(options),
            new NoOpTransactionChangeNotifier());

        await deleteUseCase.ExecuteAsync(new DeleteTransactionRequest(logResult.Id));

        var repository = new LiteDbTransactionRepository(options);
        var transaction = await repository.GetTransactionAsync(profileId, new TransactionId(logResult.Id));

        Assert.Null(transaction);
    }

    private sealed class NoOpTransactionChangeNotifier : ITransactionChangeNotifier
    {
        public Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static Category GetFirstExpenseCategory(FinanceAssistantDataOptions options, LocalProfileId profileId)
    {
        using var database = new LiteDatabase(options.DatabasePath);
        return database
            .GetCollection<CategoryDocument>("categories", BsonAutoId.Guid)
            .FindAll()
            .Where(category =>
                category.ProfileId == profileId.Value &&
                category.TransactionType == TransactionType.Expense.ToString())
            .Select(category => category.ToCategory())
            .First();
    }
}
