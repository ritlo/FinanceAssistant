using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.ProcessMessage;

namespace FinanceAssistant.Web.Assistant;

public sealed record AssistantToolPayloadDisplayModel(
    string Text,
    string ToolName,
    string KindLabel)
{
    public static AssistantToolPayloadDisplayModel? FromResult(ProcessAssistantMessageResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.PayloadJson))
        {
            return null;
        }

        var toolName = string.IsNullOrWhiteSpace(result.ToolName) ? "Assistant payload" : result.ToolName;
        return new AssistantToolPayloadDisplayModel(result.PayloadJson, toolName, GetKindLabel(result));
    }

    private static string GetKindLabel(ProcessAssistantMessageResult result)
    {
        if (result.RequiresConfirmation)
        {
            return "Needs confirmation";
        }

        return result.ToolKind == AssistantToolCallKind.Advice ? "Advice" : "Read-only";
    }
}
