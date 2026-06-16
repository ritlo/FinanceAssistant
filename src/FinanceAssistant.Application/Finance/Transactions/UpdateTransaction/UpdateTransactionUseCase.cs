using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ResolveCategory;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;

public sealed class UpdateTransactionUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly ITransactionChangeNotifier transactionChangeNotifier;

    public UpdateTransactionUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        ITransactionChangeNotifier transactionChangeNotifier)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
        this.transactionRepository = transactionRepository;
        this.transactionChangeNotifier = transactionChangeNotifier;
    }

    public async Task<TransactionResult> ExecuteAsync(
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var transaction = await transactionRepository.GetTransactionAsync(profileId, new TransactionId(request.Id), cancellationToken);

        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }

        var category = request.CategoryId.HasValue
            ? await GetExplicitCategoryAsync(profileId, request, cancellationToken)
            : await ResolveCategoryAsync(profileId, request, cancellationToken);

        transaction.Update(
            Money.Create(request.Amount),
            request.Type,
            request.Date,
            request.Description,
            category);

        await transactionRepository.UpdateTransactionAsync(transaction, cancellationToken);
        await transactionChangeNotifier.PublishTransactionChangedAsync(cancellationToken);

        return TransactionResult.FromTransaction(transaction, category);
    }

    private async Task<Category> GetExplicitCategoryAsync(
        LocalProfileId profileId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId.GetValueOrDefault());
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);
        var category = categories.SingleOrDefault(candidate => candidate.Id == categoryId);

        if (category is null)
        {
            throw new DomainValidationException("Category must exist for the local profile.");
        }

        return category;
    }

    private async Task<Category> ResolveCategoryAsync(
        LocalProfileId profileId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var resolver = new ResolveCategoryUseCase(
            new FixedCurrentProfileProvider(profileId),
            categoryRepository);
        var result = await resolver.ExecuteAsync(
            new ResolveCategoryRequest(request.Description, request.Type),
            cancellationToken);
        var categories = await categoryRepository.ListCategoriesAsync(profileId, cancellationToken);

        return categories.Single(category => category.Id.Value == result.Id);
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
}
