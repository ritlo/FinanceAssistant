using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Finance.Categories;

public sealed class CategorizationRule
{
    private CategorizationRule(
        CategorizationRuleId id,
        LocalProfileId profileId,
        string keyword,
        TransactionType transactionType,
        CategoryId categoryId,
        int order,
        bool isActive)
    {
        Id = id;
        ProfileId = profileId;
        Keyword = keyword;
        TransactionType = transactionType;
        CategoryId = categoryId;
        Order = order;
        IsActive = isActive;
    }

    public CategorizationRuleId Id { get; }

    public LocalProfileId ProfileId { get; }

    public string Keyword { get; }

    public TransactionType TransactionType { get; }

    public CategoryId CategoryId { get; }

    public int Order { get; }

    public bool IsActive { get; }

    public static CategorizationRule Create(
        LocalProfileId profileId,
        string keyword,
        TransactionType transactionType,
        Category category,
        int order,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.ProfileId != profileId)
        {
            throw new DomainValidationException("Rule category must belong to the same local profile.");
        }

        if (category.TransactionType != transactionType)
        {
            throw new DomainValidationException("Rule category must be compatible with transaction type.");
        }

        return Rehydrate(
            CategorizationRuleId.New(),
            profileId,
            keyword,
            transactionType,
            category.Id,
            order,
            isActive);
    }

    public static CategorizationRule Rehydrate(
        CategorizationRuleId id,
        LocalProfileId profileId,
        string keyword,
        TransactionType transactionType,
        CategoryId categoryId,
        int order,
        bool isActive)
    {
        if (order <= 0)
        {
            throw new DomainValidationException("Categorization rule order must be positive.");
        }

        return new CategorizationRule(
            id,
            profileId,
            RequiredText.Normalize(keyword, "Categorization keyword"),
            transactionType,
            categoryId,
            order,
            isActive);
    }

    public bool Matches(string description)
    {
        if (!IsActive)
        {
            return false;
        }

        var normalizedDescription = RequiredText.Normalize(description, "Transaction description");
        return normalizedDescription.Contains(Keyword, StringComparison.OrdinalIgnoreCase);
    }
}
