using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Finance.Transactions;

public sealed class Transaction
{
    private Transaction(
        TransactionId id,
        LocalProfileId profileId,
        Money amount,
        TransactionType type,
        DateOnly date,
        string description,
        CategoryId categoryId)
    {
        Id = id;
        ProfileId = profileId;
        Amount = amount;
        Type = type;
        Date = date;
        Description = description;
        CategoryId = categoryId;
    }

    public TransactionId Id { get; }

    public LocalProfileId ProfileId { get; }

    public Money Amount { get; private set; }

    public TransactionType Type { get; private set; }

    public DateOnly Date { get; private set; }

    public string Description { get; private set; }

    public CategoryId CategoryId { get; private set; }

    public static Transaction Create(
        LocalProfileId profileId,
        Money amount,
        TransactionType type,
        DateOnly date,
        string description,
        Category category)
    {
        return Rehydrate(TransactionId.New(), profileId, amount, type, date, description, category);
    }

    public static Transaction Rehydrate(
        TransactionId id,
        LocalProfileId profileId,
        Money amount,
        TransactionType type,
        DateOnly date,
        string description,
        Category category)
    {
        ValidateDate(date);
        ValidateCategory(profileId, type, category);

        return new Transaction(
            id,
            profileId,
            amount,
            type,
            date,
            RequiredText.Normalize(description, "Transaction description"),
            category.Id);
    }

    public void Update(Money amount, TransactionType type, DateOnly date, string description, Category category)
    {
        ValidateDate(date);
        ValidateCategory(ProfileId, type, category);

        Amount = amount;
        Type = type;
        Date = date;
        Description = RequiredText.Normalize(description, "Transaction description");
        CategoryId = category.Id;
    }

    private static void ValidateDate(DateOnly date)
    {
        if (date == default)
        {
            throw new DomainValidationException("Transaction date is required.");
        }
    }

    private static void ValidateCategory(LocalProfileId profileId, TransactionType type, Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.ProfileId != profileId)
        {
            throw new DomainValidationException("Category must belong to the same local profile.");
        }

        if (category.TransactionType != type)
        {
            throw new DomainValidationException("Category must be compatible with transaction type.");
        }
    }
}
