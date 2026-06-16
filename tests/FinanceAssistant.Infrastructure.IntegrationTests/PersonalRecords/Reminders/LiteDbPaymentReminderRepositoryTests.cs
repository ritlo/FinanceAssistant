using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.PersonalRecords.Reminders;

namespace FinanceAssistant.Infrastructure.IntegrationTests.PersonalRecords.Reminders;

[Collection("Sequential")]
public sealed class LiteDbPaymentReminderRepositoryTests
{
    [Fact]
    public async Task CreateAndListPersistReminders()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbPaymentReminderRepository(options);
        var reminder = PaymentReminder.Create(
            profileId,
            "Rent",
            new DateOnly(2026, 7, 1),
            new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero));

        await repository.AddReminderAsync(reminder);

        var reminders = await repository.ListRemindersAsync(profileId);
        var persisted = Assert.Single(reminders);
        Assert.Equal(reminder.Id, persisted.Id);
        Assert.Equal("Rent", persisted.Content);
        Assert.Equal(new DateOnly(2026, 7, 1), persisted.DueDate);
        Assert.False(persisted.IsPaid);
    }

    [Fact]
    public async Task UpdatePersistsPaidAndUnpaidStateChanges()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbPaymentReminderRepository(options);
        var reminder = PaymentReminder.Create(profileId, "Rent", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        await repository.AddReminderAsync(reminder);

        reminder.MarkPaid();
        await repository.UpdateReminderAsync(reminder);
        var paid = await repository.GetReminderAsync(profileId, reminder.Id);
        var persistedPaidState = paid!.IsPaid;

        paid.MarkUnpaid();
        await repository.UpdateReminderAsync(paid);
        var unpaid = await repository.GetReminderAsync(profileId, reminder.Id);

        Assert.True(persistedPaidState);
        Assert.False(unpaid!.IsPaid);
    }

    [Fact]
    public async Task DeleteRemovesOnlyCurrentProfileReminder()
    {
        using var directory = TemporaryDirectory.Create();
        var options = new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            Currency = "USD",
        };
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var otherProfileId = LocalProfileId.New();
        var repository = new LiteDbPaymentReminderRepository(options);
        var current = PaymentReminder.Create(profileId, "Current", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        var other = PaymentReminder.Create(otherProfileId, "Other", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        await repository.AddReminderAsync(current);
        await repository.AddReminderAsync(other);

        await repository.DeleteReminderAsync(profileId, current.Id);
        await repository.DeleteReminderAsync(profileId, other.Id);

        Assert.Empty(await repository.ListRemindersAsync(profileId));
        var otherReminders = await repository.ListRemindersAsync(otherProfileId);
        Assert.Equal(other.Id, Assert.Single(otherReminders).Id);
    }
}
