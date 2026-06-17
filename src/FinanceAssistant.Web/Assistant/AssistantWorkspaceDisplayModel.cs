using FinanceAssistant.Application.Assistant;

namespace FinanceAssistant.Web.Assistant;

public sealed record AssistantWorkspaceDisplayModel(
    string Endpoint,
    string Model,
    string LocationLabel,
    string RemoteLabel,
    string StatusPill,
    bool RequiresRemoteDisclosure,
    string? WarningMessage)
{
    public static readonly IReadOnlyList<string> QuickPrompts =
    [
        "Explain this month",
        "Find unusual spending",
        "Draft a reminder",
        "Read parsed documents"
    ];

    public static readonly IReadOnlyList<string> ImmediateReadTools =
    [
        "ReadTransactions",
        "GetMonthlySummary",
        "AnalyzeSpendingPatterns",
        "GetNotes",
        "GetPaymentReminders",
        "ReadParsedDocument"
    ];

    public static readonly IReadOnlyList<string> ConfirmedWriteTools =
    [
        "ProposeTransaction",
        "ProposeNote",
        "ProposePaymentReminder"
    ];

    public static AssistantWorkspaceDisplayModel FromDisclosure(AssistantConfigurationDisclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        return new AssistantWorkspaceDisplayModel(
            disclosure.Endpoint.ToString(),
            disclosure.Model,
            disclosure.IsRemoteEndpoint ? "Remote" : "Local",
            disclosure.IsRemoteEndpoint ? "on" : "off",
            disclosure.RequiresRemoteDisclosure ? "Remote disclosure" : "Local-first",
            disclosure.RequiresRemoteDisclosure,
            disclosure.WarningMessage);
    }
}
