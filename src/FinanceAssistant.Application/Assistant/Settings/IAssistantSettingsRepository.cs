namespace FinanceAssistant.Application.Assistant.Settings;

public interface IAssistantSettingsRepository
{
    AssistantSettings Get();

    Task SaveAsync(AssistantSettings settings, CancellationToken cancellationToken = default);
}
