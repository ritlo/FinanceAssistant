using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;

namespace FinanceAssistant.Application.Finance.Categories.AddCategorizationRule;

public sealed class AddCategorizationRuleUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public AddCategorizationRuleUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<CategorizationRuleResult> ExecuteAsync(
        AddCategorizationRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);
        var categoryId = new CategoryId(request.CategoryId);
        var category = categories.SingleOrDefault(candidate => candidate.Id == categoryId);

        if (category is null)
        {
            throw new DomainValidationException("Rule category must exist for the local profile.");
        }

        var existingRules = await categoryRepository.ListCategorizationRulesAsync(
            profileId,
            request.TransactionType,
            cancellationToken);

        if (existingRules.Any(rule => rule.Order == request.Order))
        {
            throw new DomainValidationException("Categorization rule order must be unique within transaction type.");
        }

        var rule = CategorizationRule.Create(
            profileId,
            request.Keyword,
            request.TransactionType,
            category,
            request.Order,
            request.IsActive);

        await categoryRepository.AddCategorizationRuleAsync(rule, cancellationToken);

        return CategorizationRuleResult.FromRule(rule);
    }
}
