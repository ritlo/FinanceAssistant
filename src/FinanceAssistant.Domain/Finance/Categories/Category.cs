using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Finance.Categories;

public sealed class Category
{
    private Category(CategoryId id, LocalProfileId profileId, string name, TransactionType transactionType)
    {
        Id = id;
        ProfileId = profileId;
        Name = name;
        TransactionType = transactionType;
    }

    public CategoryId Id { get; }

    public LocalProfileId ProfileId { get; }

    public string Name { get; }

    public TransactionType TransactionType { get; }

    public static Category Create(LocalProfileId profileId, string name, TransactionType transactionType)
    {
        return Rehydrate(CategoryId.New(), profileId, name, transactionType);
    }

    public static Category Rehydrate(
        CategoryId id,
        LocalProfileId profileId,
        string name,
        TransactionType transactionType)
    {
        var normalizedName = RequiredText.Normalize(name, "Category name");
        return new Category(id, profileId, normalizedName, transactionType);
    }

    public bool HasSameName(string candidateName)
    {
        var normalizedCandidate = RequiredText.Normalize(candidateName, "Category name");
        return string.Equals(Name, normalizedCandidate, StringComparison.OrdinalIgnoreCase);
    }
}
