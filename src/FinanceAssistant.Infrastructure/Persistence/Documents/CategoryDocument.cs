using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class CategoryDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public static CategoryDocument FromCategory(Category category)
    {
        return new CategoryDocument
        {
            Id = category.Id.Value,
            ProfileId = category.ProfileId.Value,
            Name = category.Name,
            TransactionType = category.TransactionType.ToString(),
        };
    }

    public Category ToCategory()
    {
        return Category.Rehydrate(
            new CategoryId(Id),
            new LocalProfileId(ProfileId),
            Name,
            Enum.Parse<TransactionType>(TransactionType));
    }
}
