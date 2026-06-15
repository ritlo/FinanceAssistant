using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.AddCategorizationRule;
using FinanceAssistant.Application.Finance.Categories.AddCategory;
using FinanceAssistant.Application.Finance.Categories.EnsureDefaultCategories;
using FinanceAssistant.Application.Finance.Categories.ListCategories;
using FinanceAssistant.Application.Finance.Categories.ListCategorizationRules;
using FinanceAssistant.Application.Finance.Categories.ResolveCategory;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Tests.Finance.Categories;

public sealed class CategoryUseCaseTests
{
    [Fact]
    public async Task EnsureDefaultCategoriesAddsMissingDefaultsWithoutDuplicatingExistingNames()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        var existingOther = repository.AddExisting(Category.Create(profileId, " other ", TransactionType.Expense));
        var useCase = new EnsureDefaultCategoriesUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository);

        var results = await useCase.ExecuteAsync();

        Assert.Equal(CategoryDefaults.All.Count, results.Count);
        Assert.Single(repository.Categories, category =>
            category.TransactionType == TransactionType.Expense && category.HasSameName("Other"));
        Assert.Contains(results, result => result.Id == existingOther.Id.Value);
        Assert.Contains(results, result =>
            result.Name == "Salary" && result.TransactionType == TransactionType.Income);
    }

    [Fact]
    public async Task AddCategoryRejectsDuplicateNamesWithinTypeButAllowsSameNameAcrossTypes()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        repository.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        var useCase = new AddCategoryUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new AddCategoryRequest(" other ", TransactionType.Expense)));
        var incomeOther = await useCase.ExecuteAsync(new AddCategoryRequest("Other", TransactionType.Income));

        Assert.Equal("Category name must be unique within transaction type.", exception.Message);
        Assert.Equal(TransactionType.Income, incomeOther.TransactionType);
    }

    [Fact]
    public async Task AddCategorizationRuleCreatesRuleForProfileCategoryAndUniqueOrder()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        var groceries = repository.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var useCase = new AddCategorizationRuleUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository);

        var result = await useCase.ExecuteAsync(
            new AddCategorizationRuleRequest(
                " market ",
                TransactionType.Expense,
                groceries.Id.Value,
                10));

        Assert.Equal("market", result.Keyword);
        Assert.Equal(groceries.Id.Value, result.CategoryId);
        Assert.Equal(10, result.Order);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task AddCategorizationRuleRejectsDuplicateOrderWithinType()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        var groceries = repository.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        repository.AddExisting(CategorizationRule.Create(
            profileId,
            "market",
            TransactionType.Expense,
            groceries,
            1));
        var useCase = new AddCategorizationRuleUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(
                new AddCategorizationRuleRequest(
                    "grocery",
                    TransactionType.Expense,
                    groceries.Id.Value,
                    1)));

        Assert.Equal("Categorization rule order must be unique within transaction type.", exception.Message);
    }

    [Fact]
    public async Task ResolveCategoryUsesFirstMatchingActiveRuleByAscendingOrder()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        var groceries = repository.AddExisting(Category.Create(profileId, "Groceries", TransactionType.Expense));
        var services = repository.AddExisting(Category.Create(profileId, "Services", TransactionType.Expense));
        repository.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        repository.AddExisting(CategorizationRule.Create(
            profileId,
            "market",
            TransactionType.Expense,
            groceries,
            20));
        repository.AddExisting(CategorizationRule.Create(
            profileId,
            "market",
            TransactionType.Expense,
            services,
            10));
        var useCase = new ResolveCategoryUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository);

        var result = await useCase.ExecuteAsync(
            new ResolveCategoryRequest("Corner MARKET purchase", TransactionType.Expense));

        Assert.Equal(services.Id.Value, result.Id);
    }

    [Fact]
    public async Task ResolveCategoryFallsBackToTypeCompatibleOtherWhenNoRuleMatches()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        repository.AddExisting(Category.Create(profileId, "Other", TransactionType.Expense));
        var incomeOther = repository.AddExisting(Category.Create(profileId, "Other", TransactionType.Income));
        var useCase = new ResolveCategoryUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository);

        var result = await useCase.ExecuteAsync(
            new ResolveCategoryRequest("Unknown payroll source", TransactionType.Income));

        Assert.Equal(incomeOther.Id.Value, result.Id);
    }

    [Fact]
    public async Task ListUseCasesReturnDeterministicOrdering()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeCategoryRepository();
        var rent = repository.AddExisting(Category.Create(profileId, "Rent", TransactionType.Expense));
        var salary = repository.AddExisting(Category.Create(profileId, "Salary", TransactionType.Income));
        repository.AddExisting(CategorizationRule.Create(
            profileId,
            "rent",
            TransactionType.Expense,
            rent,
            20));
        repository.AddExisting(CategorizationRule.Create(
            profileId,
            "apartment",
            TransactionType.Expense,
            rent,
            10));
        var profileProvider = new FixedCurrentProfileProvider(profileId);
        var categories = await new ListCategoriesUseCase(profileProvider, repository).ExecuteAsync();
        var rules = await new ListCategorizationRulesUseCase(profileProvider, repository).ExecuteAsync(
            new ListCategorizationRulesRequest(TransactionType.Expense));

        Assert.Equal([salary.Id.Value, rent.Id.Value], categories.Select(category => category.Id));
        Assert.Equal([10, 20], rules.Select(rule => rule.Order));
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

        public IReadOnlyList<Category> Categories => categories;

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
}
