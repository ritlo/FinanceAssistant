using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Finance.Categories;

public readonly record struct CategorizationRuleId
{
    public CategorizationRuleId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Categorization rule ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CategorizationRuleId New() => new(Guid.NewGuid());
}
