namespace FinanceAssistant.Application.Assistant.Settings.UpdateAssistantSettings;

public sealed record UpdateAssistantSettingsRequest(
    bool WriteProposalsEnabled,
    string EndpointUrl,
    int EndpointPort,
    bool AllowRemoteEndpoint);
