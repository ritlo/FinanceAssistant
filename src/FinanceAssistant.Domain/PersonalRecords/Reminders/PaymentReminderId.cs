using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Domain.PersonalRecords.Reminders;

public readonly record struct PaymentReminderId
{
    public PaymentReminderId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainValidationException("Payment reminder ID is required.");
        }

        Value = value;
    }

    public Guid Value { get; }

    public static PaymentReminderId New() => new(Guid.NewGuid());
}
