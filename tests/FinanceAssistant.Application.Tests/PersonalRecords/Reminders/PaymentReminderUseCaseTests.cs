using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Reminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;
using FinanceAssistant.Application.PersonalRecords.Reminders.DeleteReminder;
using FinanceAssistant.Application.PersonalRecords.Reminders.ListReminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderPaid;
using FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderUnpaid;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.Tests.PersonalRecords.Reminders;

public sealed class PaymentReminderUseCaseTests
{
    [Fact]
    public async Task CreateUsesCurrentProfileAndClock()
    {
        var profileId = LocalProfileId.New();
        var createdAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeReminderRepository();
        var useCase = new CreateReminderUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository,
            new FixedClock(createdAt));

        var result = await useCase.ExecuteAsync(new CreateReminderRequest("Rent", new DateOnly(2026, 7, 1)));

        var reminder = repository.Reminders.Single();
        Assert.Equal(profileId, reminder.ProfileId);
        Assert.Equal(createdAt, reminder.CreatedAt);
        Assert.False(reminder.IsPaid);
        Assert.Equal(reminder.Id.Value, result.Id);
    }

    [Fact]
    public async Task ListSortsUnpaidFirstThenDueDateThenCreationTime()
    {
        var profileId = LocalProfileId.New();
        var repository = new FakeReminderRepository();
        var paid = repository.AddExisting(
            profileId,
            "Paid",
            new DateOnly(2026, 6, 1),
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            isPaid: true);
        var laterUnpaid = repository.AddExisting(
            profileId,
            "Later unpaid",
            new DateOnly(2026, 6, 10),
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            isPaid: false);
        var earlierCreatedUnpaid = repository.AddExisting(
            profileId,
            "Earlier created unpaid",
            new DateOnly(2026, 6, 5),
            new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero),
            isPaid: false);
        var laterCreatedUnpaid = repository.AddExisting(
            profileId,
            "Later created unpaid",
            new DateOnly(2026, 6, 5),
            new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero),
            isPaid: false);
        var useCase = new ListRemindersUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var result = await useCase.ExecuteAsync();

        Assert.Collection(
            result,
            reminder => Assert.Equal(earlierCreatedUnpaid.Id.Value, reminder.Id),
            reminder => Assert.Equal(laterCreatedUnpaid.Id.Value, reminder.Id),
            reminder => Assert.Equal(laterUnpaid.Id.Value, reminder.Id),
            reminder => Assert.Equal(paid.Id.Value, reminder.Id));
    }

    [Fact]
    public async Task DeletePreventsCrossProfileAccess()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeReminderRepository();
        var current = repository.AddExisting(profileId, "Current", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow, false);
        var other = repository.AddExisting(otherProfileId, "Other", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow, false);
        var useCase = new DeleteReminderUseCase(new FixedCurrentProfileProvider(profileId), repository);

        await useCase.ExecuteAsync(new DeleteReminderRequest(current.Id.Value));
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new DeleteReminderRequest(other.Id.Value)));

        Assert.Equal("Payment reminder was not found.", exception.Message);
        Assert.Empty(await repository.ListRemindersAsync(profileId));
        Assert.Single(await repository.ListRemindersAsync(otherProfileId));
    }

    [Fact]
    public async Task MarkPaidAndUnpaidPreventCrossProfileAccess()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeReminderRepository();
        var current = repository.AddExisting(profileId, "Current", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow, false);
        var other = repository.AddExisting(otherProfileId, "Other", new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow, false);
        var markPaid = new MarkReminderPaidUseCase(new FixedCurrentProfileProvider(profileId), repository);
        var markUnpaid = new MarkReminderUnpaidUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var paid = await markPaid.ExecuteAsync(new MarkReminderPaidRequest(current.Id.Value));
        var unpaid = await markUnpaid.ExecuteAsync(new MarkReminderUnpaidRequest(current.Id.Value));
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => markPaid.ExecuteAsync(new MarkReminderPaidRequest(other.Id.Value)));

        Assert.True(paid.IsPaid);
        Assert.False(unpaid.IsPaid);
        Assert.Equal("Payment reminder was not found.", exception.Message);
    }

    private sealed class FixedCurrentProfileProvider : ICurrentProfileProvider
    {
        private readonly LocalProfileId profileId;

        public FixedCurrentProfileProvider(LocalProfileId profileId)
        {
            this.profileId = profileId;
        }

        public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(profileId);
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeReminderRepository : IReminderRepository
    {
        public List<PaymentReminder> Reminders { get; } = [];

        public PaymentReminder AddExisting(
            LocalProfileId profileId,
            string content,
            DateOnly dueDate,
            DateTimeOffset createdAt,
            bool isPaid)
        {
            var reminder = PaymentReminder.Rehydrate(
                PaymentReminderId.New(),
                profileId,
                content,
                dueDate,
                createdAt,
                isPaid);
            Reminders.Add(reminder);
            return reminder;
        }

        public Task AddReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
        {
            Reminders.Add(reminder);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PaymentReminder>> ListRemindersAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PaymentReminder>>(
                Reminders.Where(reminder => reminder.ProfileId == profileId).ToArray());
        }

        public Task<PaymentReminder?> GetReminderAsync(
            LocalProfileId profileId,
            PaymentReminderId reminderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaymentReminder?>(
                Reminders.SingleOrDefault(reminder => reminder.ProfileId == profileId && reminder.Id == reminderId));
        }

        public Task UpdateReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteReminderAsync(
            LocalProfileId profileId,
            PaymentReminderId reminderId,
            CancellationToken cancellationToken = default)
        {
            var reminder = Reminders.SingleOrDefault(candidate =>
                candidate.ProfileId == profileId && candidate.Id == reminderId);
            if (reminder is not null)
            {
                Reminders.Remove(reminder);
            }

            return Task.CompletedTask;
        }
    }
}
