namespace FinanceAssistant.Infrastructure.Persistence;

public sealed class FinanceAssistantDataOptions
{
    public string DatabasePath { get; init; } = DefaultDatabasePath();

    public string Currency { get; init; } = string.Empty;

    public static string DefaultDatabasePath()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
        {
            localData = Path.Combine(Path.GetTempPath(), "FinanceAssistant");
        }

        return Path.Combine(localData, "FinanceAssistant", "FinanceAssistant.db");
    }
}
