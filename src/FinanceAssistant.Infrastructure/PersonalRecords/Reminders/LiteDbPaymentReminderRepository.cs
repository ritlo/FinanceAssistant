using FinanceAssistant.Application.PersonalRecords.Reminders;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.PersonalRecords.Reminders;

public sealed class LiteDbPaymentReminderRepository : IReminderRepository
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbPaymentReminderRepository(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public Task AddReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .Insert(PaymentReminderDocument.FromReminder(reminder));

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentReminder>> ListRemindersAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var reminders = database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .FindAll()
            .Where(reminder => reminder.ProfileId == profileId.Value)
            .Select(reminder => reminder.ToReminder())
            .ToArray();

        return Task.FromResult<IReadOnlyList<PaymentReminder>>(reminders);
    }

    public Task<PaymentReminder?> GetReminderAsync(
        LocalProfileId profileId,
        PaymentReminderId reminderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .FindById(reminderId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.FromResult<PaymentReminder?>(null);
        }

        return Task.FromResult<PaymentReminder?>(document.ToReminder());
    }

    public Task UpdateReminderAsync(
        PaymentReminder reminder,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .FindById(reminder.Id.Value);

        if (document is null || document.ProfileId != reminder.ProfileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .Update(document.Id, PaymentReminderDocument.FromReminder(reminder));

        return Task.CompletedTask;
    }

    public Task DeleteReminderAsync(
        LocalProfileId profileId,
        PaymentReminderId reminderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var document = database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .FindById(reminderId.Value);

        if (document is null || document.ProfileId != profileId.Value)
        {
            return Task.CompletedTask;
        }

        database
            .GetCollection<PaymentReminderDocument>(LiteDbCollectionNames.PaymentReminders, BsonAutoId.Guid)
            .Delete(reminderId.Value);

        return Task.CompletedTask;
    }
}
