using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Finance.Transactions;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;
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
        var repository = new LiteDbTransactionRepository(options);

        var result = await repository.ListTransactionsAsync(profileId);

        Assert.Empty(result);
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
        var repository = new LiteDbTransactionRepository(options);

        var transaction = await repository.GetTransactionAsync(otherProfileId, new TransactionId(Guid.NewGuid()));

        Assert.Null(transaction);
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
}
