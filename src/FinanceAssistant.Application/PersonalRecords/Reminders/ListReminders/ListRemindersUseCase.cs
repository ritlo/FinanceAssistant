using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.PersonalRecords.Reminders.ListReminders;

public sealed class ListRemindersUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IReminderRepository reminderRepository;

    public ListRemindersUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IReminderRepository reminderRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.reminderRepository = reminderRepository;
    }

    public async Task<IReadOnlyList<PaymentReminderResult>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var reminders = await reminderRepository.ListRemindersAsync(profileId, cancellationToken);

        return reminders
            .OrderBy(reminder => reminder.IsPaid)
            .ThenBy(reminder => reminder.DueDate)
            .ThenBy(reminder => reminder.CreatedAt)
            .Select(PaymentReminderResult.FromReminder)
            .ToArray();
    }
}
