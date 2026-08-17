using System.Globalization;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class PaymentReminderDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    public string Content { get; set; } = string.Empty;

    public string DueDate { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    public bool IsPaid { get; set; }

    public static PaymentReminderDocument FromReminder(PaymentReminder reminder)
    {
        return new PaymentReminderDocument
        {
            Id = reminder.Id.Value,
            ProfileId = reminder.ProfileId.Value,
            Content = reminder.Content,
            DueDate = reminder.DueDate.ToString("O", CultureInfo.InvariantCulture),
            CreatedAt = reminder.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            IsPaid = reminder.IsPaid,
        };
    }

    public PaymentReminder ToReminder()
    {
        return PaymentReminder.Rehydrate(
            new PaymentReminderId(Id),
            new LocalProfileId(ProfileId),
            Content,
            DateOnly.ParseExact(DueDate, "O", CultureInfo.InvariantCulture),
            DateTimeOffset.ParseExact(CreatedAt, "O", CultureInfo.InvariantCulture),
            IsPaid);
    }
}
