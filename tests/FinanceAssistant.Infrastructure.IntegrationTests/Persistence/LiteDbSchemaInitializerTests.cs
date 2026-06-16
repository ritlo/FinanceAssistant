using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Persistence;

[Collection("Sequential")]
public sealed class LiteDbSchemaInitializerTests
{
    [Fact]
    public async Task InitializeCreatesMetadataProfileAndDefaultCategories()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var initializer = new LiteDbSchemaInitializer(options);

        var profileId = initializer.Initialize();
        var provider = new LiteDbCurrentProfileProvider(options);
        var repository = new LiteDbCategoryRepository(options);
        var categories = await repository.ListCategoriesAsync(profileId);

        Assert.Equal(profileId, await provider.GetCurrentProfileIdAsync());
        Assert.Equal(CategoryDefaults.All.Count, categories.Count);
        Assert.Contains(categories, category =>
            category.Name == "Other" && category.TransactionType == TransactionType.Expense);
        Assert.Contains(categories, category =>
            category.Name == "Salary" && category.TransactionType == TransactionType.Income);
    }

    [Fact]
    public void InitializeRejectsCurrencyMismatchWithoutReinitializingDatabase()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory, "USD");
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var mismatchedOptions = CreateOptions(directory, "GBP");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new LiteDbSchemaInitializer(mismatchedOptions).Initialize());
        var originalProfileId = new LiteDbSchemaInitializer(options).Initialize();

        Assert.Equal("Configured currency does not match database currency.", exception.Message);
        Assert.Equal(profileId, originalProfileId);
    }

    [Fact]
    public void InitializeRejectsExistingDatabaseWithoutMetadata()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        File.WriteAllText(options.DatabasePath, string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new LiteDbSchemaInitializer(options).Initialize());

        Assert.Equal("Existing database is missing FinanceAssistant metadata.", exception.Message);
    }

    private static FinanceAssistantDataOptions CreateOptions(TemporaryDirectory directory, string currency = "USD")
    {
        return new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = currency,
        };
    }
}
