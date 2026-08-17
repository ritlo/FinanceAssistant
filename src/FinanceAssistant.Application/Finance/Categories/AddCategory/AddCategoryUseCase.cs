using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;

namespace FinanceAssistant.Application.Finance.Categories.AddCategory;

public sealed class AddCategoryUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public AddCategoryUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<CategoryResult> ExecuteAsync(
        AddCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var existingCategories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);

        if (existingCategories.Any(category =>
                category.TransactionType == request.TransactionType && category.HasSameName(request.Name)))
        {
            throw new DomainValidationException("Category name must be unique within transaction type.");
        }

        var category = Category.Create(profileId, request.Name, request.TransactionType);
        await categoryRepository.AddCategoryAsync(category, cancellationToken);

        return CategoryResult.FromCategory(category);
    }
}
