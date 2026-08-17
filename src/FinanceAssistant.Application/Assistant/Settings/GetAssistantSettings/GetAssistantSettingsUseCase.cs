namespace FinanceAssistant.Application.Assistant.Settings.GetAssistantSettings;

public sealed class GetAssistantSettingsUseCase
{
    private readonly IAssistantSettingsRepository settingsRepository;

    public GetAssistantSettingsUseCase(IAssistantSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public AssistantSettings Execute()
    {
        return settingsRepository.Get();
    }
}
