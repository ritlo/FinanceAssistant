namespace FinanceAssistant.Application.Assistant;

public sealed class FileAssistantPromptCatalog : IAssistantPromptCatalog
{
    private const string PromptRelativePath = "Assistant/Prompts/v1/system.md";
    private const string SchemaRelativeDirectory = "Assistant/ToolSchemas/v1";

    public async Task<string> GetSystemPromptAsync(CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(
            ResolvePath(PromptRelativePath),
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetToolSchemasAsync(CancellationToken cancellationToken = default)
    {
        var schemaDirectory = ResolvePath(SchemaRelativeDirectory);
        var schemas = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var toolName in AssistantToolNames.All)
        {
            var path = Path.Combine(schemaDirectory, $"{toolName}.json");
            schemas[toolName] = await File.ReadAllTextAsync(path, cancellationToken);
        }

        return schemas;
    }

    private static string ResolvePath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, relativePath);
    }
}
