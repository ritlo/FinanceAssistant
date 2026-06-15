using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Finance.Categories;

public readonly record struct CategoryId
{
    public CategoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Category ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static CategoryId New() => new(Guid.NewGuid());
}
