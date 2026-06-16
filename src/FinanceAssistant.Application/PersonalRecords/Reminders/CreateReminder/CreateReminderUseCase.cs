using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;

public sealed class CreateReminderUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IReminderRepository reminderRepository;
    private readonly IClock clock;

    public CreateReminderUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IReminderRepository reminderRepository,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.reminderRepository = reminderRepository;
        this.clock = clock;
    }

    public async Task<PaymentReminderResult> ExecuteAsync(
        CreateReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var reminder = PaymentReminder.Create(profileId, request.Content, request.DueDate, clock.UtcNow);

        await reminderRepository.AddReminderAsync(reminder, cancellationToken);

        return PaymentReminderResult.FromReminder(reminder);
    }
}
