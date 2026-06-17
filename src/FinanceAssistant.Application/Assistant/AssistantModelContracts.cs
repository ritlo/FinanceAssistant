namespace FinanceAssistant.Application.Assistant;

public sealed record AssistantModelRequest(
    string SystemPrompt,
    string UserMessage,
    IReadOnlyDictionary<string, string> ToolSchemas,
    string RuntimeContext = "");

public sealed record AssistantConfigurationDisclosure(
    Uri Endpoint,
    string Model,
    bool IsRemoteEndpoint,
    bool IsRemoteAllowed,
    bool RequiresRemoteDisclosure,
    string? WarningMessage);

public sealed record AssistantModelResponse(
    bool IsAvailable,
    string? Content,
    string? ErrorMessage,
    AssistantConfigurationDisclosure Configuration)
{
    public static AssistantModelResponse Available(
        string content,
        AssistantConfigurationDisclosure configuration)
    {
        return new AssistantModelResponse(
            IsAvailable: true,
            content,
            ErrorMessage: null,
            configuration);
    }

    public static AssistantModelResponse Unavailable(
        string errorMessage,
        AssistantConfigurationDisclosure configuration)
    {
        return new AssistantModelResponse(
            IsAvailable: false,
            Content: null,
            errorMessage,
            configuration);
    }
}

public interface IAssistantModelClient
{
    AssistantConfigurationDisclosure GetConfigurationDisclosure();

    Task<AssistantModelResponse> CompleteAsync(
        AssistantModelRequest request,
        CancellationToken cancellationToken = default);
}
