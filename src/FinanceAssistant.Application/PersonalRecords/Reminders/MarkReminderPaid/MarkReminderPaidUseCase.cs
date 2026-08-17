using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderPaid;

public sealed class MarkReminderPaidUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IReminderRepository reminderRepository;

    public MarkReminderPaidUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IReminderRepository reminderRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.reminderRepository = reminderRepository;
    }

    public async Task<PaymentReminderResult> ExecuteAsync(
        MarkReminderPaidRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var reminder = await reminderRepository.GetReminderAsync(profileId, new PaymentReminderId(request.Id), cancellationToken);

        if (reminder is null)
        {
            throw new DomainValidationException("Payment reminder was not found.");
        }

        reminder.MarkPaid();
        await reminderRepository.UpdateReminderAsync(reminder, cancellationToken);

        return PaymentReminderResult.FromReminder(reminder);
    }
}
