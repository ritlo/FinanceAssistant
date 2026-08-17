using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class CategorizationRuleDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string Keyword { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; }

    public static CategorizationRuleDocument FromRule(CategorizationRule rule)
    {
        return new CategorizationRuleDocument
        {
            Id = rule.Id.Value,
            ProfileId = rule.ProfileId.Value,
            Keyword = rule.Keyword,
            TransactionType = rule.TransactionType.ToString(),
            CategoryId = rule.CategoryId.Value,
            Order = rule.Order,
            IsActive = rule.IsActive,
        };
    }

    public CategorizationRule ToRule()
    {
        return CategorizationRule.Rehydrate(
            new CategorizationRuleId(Id),
            new LocalProfileId(ProfileId),
            Keyword,
            Enum.Parse<TransactionType>(TransactionType),
            new CategoryId(CategoryId),
            Order,
            IsActive);
    }
}
