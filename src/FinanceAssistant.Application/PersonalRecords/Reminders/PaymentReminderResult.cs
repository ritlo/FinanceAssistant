using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders;

public sealed record PaymentReminderResult(
    Guid Id,
    string Content,
    DateOnly DueDate,
    DateTimeOffset CreatedAt,
    bool IsPaid)
{
    public static PaymentReminderResult FromReminder(PaymentReminder reminder)
    {
        return new PaymentReminderResult(
            reminder.Id.Value,
            reminder.Content,
            reminder.DueDate,
            reminder.CreatedAt,
            reminder.IsPaid);
    }
}
