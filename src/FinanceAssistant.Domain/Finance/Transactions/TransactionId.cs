using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.Finance.Transactions;

public readonly record struct TransactionId
{
    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Transaction ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static TransactionId New() => new(Guid.NewGuid());
}
