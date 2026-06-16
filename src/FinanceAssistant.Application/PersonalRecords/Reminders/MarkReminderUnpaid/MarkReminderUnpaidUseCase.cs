using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderUnpaid;

public sealed class MarkReminderUnpaidUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IReminderRepository reminderRepository;

    public MarkReminderUnpaidUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IReminderRepository reminderRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.reminderRepository = reminderRepository;
    }

    public async Task<PaymentReminderResult> ExecuteAsync(
        MarkReminderUnpaidRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var reminder = await reminderRepository.GetReminderAsync(profileId, new PaymentReminderId(request.Id), cancellationToken);

        if (reminder is null)
        {
            throw new DomainValidationException("Payment reminder was not found.");
        }

        reminder.MarkUnpaid();
        await reminderRepository.UpdateReminderAsync(reminder, cancellationToken);

        return PaymentReminderResult.FromReminder(reminder);
    }
}
