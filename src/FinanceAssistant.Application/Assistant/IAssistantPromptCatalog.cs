namespace FinanceAssistant.Application.Assistant;

public interface IAssistantPromptCatalog
{
    Task<string> GetSystemPromptAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetToolSchemasAsync(CancellationToken cancellationToken = default);
}
