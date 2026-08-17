using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Finance.Categories;

namespace FinanceAssistant.Application.Finance.Categories.EnsureDefaultCategories;

public sealed class EnsureDefaultCategoriesUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public EnsureDefaultCategoriesUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var existingCategories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);
        var results = new List<CategoryResult>();

        foreach (var defaultCategory in CategoryDefaults.All)
        {
            var category = existingCategories.SingleOrDefault(candidate =>
                candidate.TransactionType == defaultCategory.TransactionType
                && candidate.HasSameName(defaultCategory.Name));

            if (category is null)
            {
                category = Category.Create(
                    profileId,
                    defaultCategory.Name,
                    defaultCategory.TransactionType);

                await categoryRepository.AddCategoryAsync(category, cancellationToken);
                existingCategories = [.. existingCategories, category];
            }

            results.Add(CategoryResult.FromCategory(category));
        }

        return results;
    }
}
