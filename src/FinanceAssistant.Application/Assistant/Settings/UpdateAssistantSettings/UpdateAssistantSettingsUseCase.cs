using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Application.Assistant.Settings.UpdateAssistantSettings;

public sealed class UpdateAssistantSettingsUseCase
{
    private readonly IAssistantSettingsRepository settingsRepository;

    public UpdateAssistantSettingsUseCase(IAssistantSettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    public async Task<AssistantSettings> ExecuteAsync(
        UpdateAssistantSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AssistantSettings.TryParseEndpoint(request.EndpointUrl, out var endpoint))
        {
            throw new DomainValidationException("Assistant endpoint URL must be an absolute HTTP or HTTPS URL.");
        }

        if (request.EndpointPort is < 1 or > 65535)
        {
            throw new DomainValidationException("Assistant endpoint port must be between 1 and 65535.");
        }

        var settings = new AssistantSettings(
            request.WriteProposalsEnabled,
            AssistantSettings.EndpointUrlWithoutPort(endpoint),
            request.EndpointPort,
            request.AllowRemoteEndpoint);

        await settingsRepository.SaveAsync(settings, cancellationToken);
        return settings;
    }
}
