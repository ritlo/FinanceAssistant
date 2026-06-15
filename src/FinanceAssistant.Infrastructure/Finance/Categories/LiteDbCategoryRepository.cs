using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;

namespace FinanceAssistant.Infrastructure.Finance.Categories;

public sealed class LiteDbCategoryRepository : ICategoryRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbCategoryRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task<IReadOnlyList<Category>> ListCategoriesAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var categories = database
            .GetCollection<CategoryDocument>(LiteDbCollectionNames.Categories, LiteDB.BsonAutoId.Guid)
            .FindAll()
            .Where(category => category.ProfileId == profileId.Value)
            .Select(category => category.ToCategory())
            .ToArray();

        return Task.FromResult<IReadOnlyList<Category>>(categories);
    }

    public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<CategoryDocument>(LiteDbCollectionNames.Categories, LiteDB.BsonAutoId.Guid)
            .Insert(CategoryDocument.FromCategory(category));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CategorizationRule>> ListCategorizationRulesAsync(
        LocalProfileId profileId,
        TransactionType transactionType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var rules = database
            .GetCollection<CategorizationRuleDocument>(
                LiteDbCollectionNames.CategorizationRules,
                LiteDB.BsonAutoId.Guid)
            .FindAll()
            .Where(rule =>
                rule.ProfileId == profileId.Value && rule.TransactionType == transactionType.ToString())
            .Select(rule => rule.ToRule())
            .ToArray();

        return Task.FromResult<IReadOnlyList<CategorizationRule>>(rules);
    }

    public Task AddCategorizationRuleAsync(
        CategorizationRule rule,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<CategorizationRuleDocument>(
                LiteDbCollectionNames.CategorizationRules,
                LiteDB.BsonAutoId.Guid)
            .Insert(CategorizationRuleDocument.FromRule(rule));

        return Task.CompletedTask;
    }
}
