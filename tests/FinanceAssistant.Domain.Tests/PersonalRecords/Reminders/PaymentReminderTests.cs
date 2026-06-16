using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Domain.Tests.PersonalRecords.Reminders;

public sealed class PaymentReminderTests
{
    [Fact]
    public void CreateDefaultsToUnpaid()
    {
        var reminder = PaymentReminder.Create(
            LocalProfileId.New(),
            "Council tax",
            new DateOnly(2026, 7, 1),
            DateTimeOffset.UtcNow);

        Assert.False(reminder.IsPaid);
    }

    [Fact]
    public void CreateRejectsEmptyContent()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => PaymentReminder.Create(
                LocalProfileId.New(),
                " \t ",
                new DateOnly(2026, 7, 1),
                DateTimeOffset.UtcNow));

        Assert.Equal("Reminder content is required.", exception.Message);
    }

    [Fact]
    public void CreateRejectsDefaultDueDate()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => PaymentReminder.Create(
                LocalProfileId.New(),
                "Council tax",
                default,
                DateTimeOffset.UtcNow));

        Assert.Equal("Reminder due date is required.", exception.Message);
    }

    [Fact]
    public void MarkPaidAndUnpaidChangesState()
    {
        var reminder = PaymentReminder.Create(
            LocalProfileId.New(),
            "Council tax",
            new DateOnly(2026, 7, 1),
            DateTimeOffset.UtcNow);

        reminder.MarkPaid();
        Assert.True(reminder.IsPaid);

        reminder.MarkUnpaid();
        Assert.False(reminder.IsPaid);
    }
}
