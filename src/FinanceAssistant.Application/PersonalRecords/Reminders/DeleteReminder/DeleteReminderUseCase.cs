using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders.DeleteReminder;

public sealed class DeleteReminderUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IReminderRepository reminderRepository;

    public DeleteReminderUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IReminderRepository reminderRepository)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.reminderRepository = reminderRepository;
    }

    public async Task ExecuteAsync(
        DeleteReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var reminderId = new PaymentReminderId(request.Id);
        var reminder = await reminderRepository.GetReminderAsync(profileId, reminderId, cancellationToken);

        if (reminder is null)
        {
            throw new DomainValidationException("Payment reminder was not found.");
        }

        await reminderRepository.DeleteReminderAsync(profileId, reminderId, cancellationToken);
    }
}
