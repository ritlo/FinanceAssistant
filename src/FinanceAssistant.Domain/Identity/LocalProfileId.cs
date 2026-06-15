using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Identity;

public readonly record struct LocalProfileId
{
    public LocalProfileId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Local profile ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static LocalProfileId New() => new(Guid.NewGuid());
}
