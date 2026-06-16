using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class TransactionDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public decimal Amount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public static TransactionDocument FromTransaction(Transaction transaction)
    {
        return new TransactionDocument
        {
            Id = transaction.Id.Value,
            ProfileId = transaction.ProfileId.Value,
            Amount = transaction.Amount.Amount,
            Type = transaction.Type.ToString(),
            Date = transaction.Date.ToString("O"),
            Description = transaction.Description,
            CategoryId = transaction.CategoryId.Value,
        };
    }

    public Transaction ToTransaction()
    {
        return Transaction.Rehydrate(
            new TransactionId(Id),
            new LocalProfileId(ProfileId),
            Money.Create(Amount),
            Enum.Parse<TransactionType>(Type),
            DateOnly.ParseExact(Date, "O"),
            Description,
            new CategoryId(CategoryId));
    }
}
