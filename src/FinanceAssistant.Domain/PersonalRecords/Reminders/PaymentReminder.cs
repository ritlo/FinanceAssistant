using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.PersonalRecords.Reminders;

public sealed class PaymentReminder
{
    private PaymentReminder(
        PaymentReminderId id,
        LocalProfileId profileId,
        string content,
        DateOnly dueDate,
        DateTimeOffset createdAt,
        bool isPaid)
    {
        Id = id;
        ProfileId = profileId;
        Content = content;
        DueDate = dueDate;
        CreatedAt = createdAt;
        IsPaid = isPaid;
    }

    public PaymentReminderId Id { get; }

    public LocalProfileId ProfileId { get; }

    public string Content { get; }

    public DateOnly DueDate { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsPaid { get; private set; }

    public static PaymentReminder Create(
        LocalProfileId profileId,
        string content,
        DateOnly dueDate,
        DateTimeOffset createdAt)
    {
        return Rehydrate(PaymentReminderId.New(), profileId, content, dueDate, createdAt, isPaid: false);
    }

    public static PaymentReminder Rehydrate(
        PaymentReminderId id,
        LocalProfileId profileId,
        string content,
        DateOnly dueDate,
        DateTimeOffset createdAt,
        bool isPaid)
    {
        ValidateDueDate(dueDate);

        return new PaymentReminder(
            id,
            profileId,
            RequiredText.Normalize(content, "Reminder content"),
            dueDate,
            createdAt,
            isPaid);
    }

    public void MarkPaid()
    {
        IsPaid = true;
    }

    public void MarkUnpaid()
    {
        IsPaid = false;
    }

    private static void ValidateDueDate(DateOnly dueDate)
    {
        if (dueDate == default)
        {
            throw new DomainValidationException("Reminder due date is required.");
        }
    }
}
