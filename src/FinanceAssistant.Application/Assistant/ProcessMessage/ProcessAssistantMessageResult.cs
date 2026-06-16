namespace FinanceAssistant.Application.Assistant.ProcessMessage;

public sealed record ProcessAssistantMessageResult(
    bool Succeeded,
    string Message,
    string? ToolName,
    AssistantToolCallKind? ToolKind,
    string? PayloadJson,
    Guid? ConfirmationToken,
    string? OperationFingerprint)
{
    public bool RequiresConfirmation => ConfirmationToken is not null;

    public static ProcessAssistantMessageResult Success(
        string message,
        string toolName,
        AssistantToolCallKind toolKind,
        string payloadJson,
        Guid? confirmationToken = null,
        string? operationFingerprint = null)
    {
        return new ProcessAssistantMessageResult(
            Succeeded: true,
            message,
            toolName,
            toolKind,
            payloadJson,
            confirmationToken,
            operationFingerprint);
    }

    public static ProcessAssistantMessageResult Error(string message)
    {
        return new ProcessAssistantMessageResult(
            Succeeded: false,
            message,
            ToolName: null,
            ToolKind: null,
            PayloadJson: null,
            ConfirmationToken: null,
            OperationFingerprint: null);
    }
}
