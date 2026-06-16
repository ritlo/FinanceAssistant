using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders;

public interface IReminderRepository
{
    Task AddReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentReminder>> ListRemindersAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default);

    Task<PaymentReminder?> GetReminderAsync(
        LocalProfileId profileId,
        PaymentReminderId reminderId,
        CancellationToken cancellationToken = default);

    Task UpdateReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default);

    Task DeleteReminderAsync(
        LocalProfileId profileId,
        PaymentReminderId reminderId,
        CancellationToken cancellationToken = default);
}
