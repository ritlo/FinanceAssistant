namespace FinanceAssistant.Application.Assistant;

public sealed record AssistantModelRequest(
    string SystemPrompt,
    string UserMessage,
    IReadOnlyDictionary<string, string> ToolSchemas);

public interface IAssistantModelClient
{
    Task<string> CompleteAsync(
        AssistantModelRequest request,
        CancellationToken cancellationToken = default);
}
