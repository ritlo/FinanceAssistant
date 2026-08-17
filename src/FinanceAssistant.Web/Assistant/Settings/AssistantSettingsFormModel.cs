using System.ComponentModel.DataAnnotations;
using FinanceAssistant.Application.Assistant.Settings;
using FinanceAssistant.Application.Assistant.Settings.UpdateAssistantSettings;

namespace FinanceAssistant.Web.Assistant.Settings;

public sealed class AssistantSettingsFormModel
{
    [Display(Name = "Enable assistant write proposals")]
    public bool WriteProposalsEnabled { get; set; }

    [Required]
    [Display(Name = "LLM API URL")]
    public string EndpointUrl { get; set; } = string.Empty;

    [Range(1, 65535)]
    [Display(Name = "LLM API port")]
    public int EndpointPort { get; set; }

    [Display(Name = "Allow remote endpoint")]
    public bool AllowRemoteEndpoint { get; set; }

    public static AssistantSettingsFormModel FromSettings(AssistantSettings settings)
    {
        return new AssistantSettingsFormModel
        {
            WriteProposalsEnabled = settings.WriteProposalsEnabled,
            EndpointUrl = settings.EndpointUrl,
            EndpointPort = settings.EndpointPort,
            AllowRemoteEndpoint = settings.AllowRemoteEndpoint,
        };
    }

    public UpdateAssistantSettingsRequest ToRequest()
    {
        return new UpdateAssistantSettingsRequest(
            WriteProposalsEnabled,
            EndpointUrl,
            EndpointPort,
            AllowRemoteEndpoint);
    }
}
