using FinanceAssistant.Application.Assistant.Settings;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class AssistantSettingsDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public bool WriteProposalsEnabled { get; set; }

    public string EndpointUrl { get; set; } = string.Empty;

    public int EndpointPort { get; set; }

    public bool AllowRemoteEndpoint { get; set; }

    public static AssistantSettingsDocument FromSettings(string id, AssistantSettings settings)
    {
        return new AssistantSettingsDocument
        {
            Id = id,
            WriteProposalsEnabled = settings.WriteProposalsEnabled,
            EndpointUrl = settings.EndpointUrl,
            EndpointPort = settings.EndpointPort,
            AllowRemoteEndpoint = settings.AllowRemoteEndpoint,
        };
    }

    public AssistantSettings ToSettings()
    {
        return new AssistantSettings(
            WriteProposalsEnabled,
            EndpointUrl,
            EndpointPort,
            AllowRemoteEndpoint);
    }
}
