using FinanceAssistant.Application.Assistant.ProcessMessage;

namespace FinanceAssistant.Web.Assistant;

public sealed record AssistantProposalPreviewModel(
    Guid Token,
    string OperationFingerprint,
    string ToolName,
    string TokenDisplay,
    string FingerprintDisplay)
{
    public static AssistantProposalPreviewModel? FromResult(ProcessAssistantMessageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.ConfirmationToken is not { } token || string.IsNullOrWhiteSpace(result.OperationFingerprint))
        {
            return null;
        }

        var toolName = string.IsNullOrWhiteSpace(result.ToolName) ? "Assistant proposal" : result.ToolName;

        return new AssistantProposalPreviewModel(
            token,
            result.OperationFingerprint,
            toolName,
            $"token {token}",
            $"fingerprint {result.OperationFingerprint}");
    }
}
