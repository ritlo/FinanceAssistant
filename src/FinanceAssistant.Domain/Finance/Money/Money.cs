using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Finance;

public readonly record struct Money
{
    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Create(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainValidationException("Amount must be positive.");
        }

        var rounded = decimal.Round(amount, 2);
        if (rounded != amount)
        {
            throw new DomainValidationException("Amount must have at most two fractional digits.");
        }

        return new Money(rounded);
    }

    public override string ToString() => Amount.ToString("0.00");
}
