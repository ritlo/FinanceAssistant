namespace FinanceAssistant.Infrastructure.Assistant;

public sealed class AssistantModelOptions
{
    public const string DefaultEndpoint = "http://localhost:11434/v1/chat/completions";
    public const string DefaultModel = "local";

    public string Endpoint { get; init; } = DefaultEndpoint;

    public string Model { get; init; } = DefaultModel;

    public string? ApiKey { get; init; }

    public bool AllowRemote { get; init; }
}
