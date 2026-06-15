using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.Finance.Categories.ListCategories;

public sealed class ListCategoriesUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public ListCategoriesUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);

        return categories
            .OrderBy(category => category.TransactionType)
            .ThenBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(CategoryResult.FromCategory)
            .ToArray();
    }
}
