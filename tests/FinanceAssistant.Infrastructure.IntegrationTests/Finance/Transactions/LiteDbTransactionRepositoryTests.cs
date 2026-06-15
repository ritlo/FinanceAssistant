using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Finance.Transactions;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using LiteDB;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Finance.Transactions;

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

    private sealed class NoOpTransactionChangeNotifier : ITransactionChangeNotifier
    {
        public Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
