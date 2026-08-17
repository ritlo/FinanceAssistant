using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Application.Finance.Categories.ResolveCategory;

public sealed class ResolveCategoryUseCase
{
    private const string FallbackCategoryName = "Other";

    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public ResolveCategoryUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<CategoryResult> ExecuteAsync(
        ResolveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var rules = await categoryRepository.ListCategorizationRulesAsync(
            profileId,
            request.TransactionType,
            cancellationToken);

        var matchingRule = rules
            .OrderBy(rule => rule.Order)
            .FirstOrDefault(rule => rule.Matches(request.Description));

        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);

        if (matchingRule is not null)
        {
            var matchedCategory = categories.SingleOrDefault(category => category.Id == matchingRule.CategoryId);
            if (matchedCategory is null)
            {
                throw new DomainValidationException("Matching categorization rule points to a missing category.");
            }

            return CategoryResult.FromCategory(matchedCategory);
        }

        var fallbackCategory = categories.SingleOrDefault(category =>
            category.TransactionType == request.TransactionType
            && category.HasSameName(FallbackCategoryName));

        if (fallbackCategory is null)
        {
            throw new DomainValidationException("Type-compatible Other category is required.");
        }

        return CategoryResult.FromCategory(fallbackCategory);
    }
}
