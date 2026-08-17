using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.Finance.Categories.ListCategorizationRules;

public sealed class ListCategorizationRulesUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly ICategoryRepository categoryRepository;

    public ListCategorizationRulesUseCase(
        ICurrentProfileProvider currentProfileProvider,
        ICategoryRepository categoryRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategorizationRuleResult>> ExecuteAsync(
        ListCategorizationRulesRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var rules = await categoryRepository.ListCategorizationRulesAsync(
            profileId,
            request.TransactionType,
            cancellationToken);

        return rules
            .OrderBy(rule => rule.Order)
            .Select(CategorizationRuleResult.FromRule)
            .ToArray();
    }
}
