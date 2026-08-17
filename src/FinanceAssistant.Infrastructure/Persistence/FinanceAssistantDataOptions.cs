namespace FinanceAssistant.Infrastructure.Persistence;

public sealed class FinanceAssistantDataOptions
{
    public string DatabasePath { get; init; } = DefaultDatabasePath();

    public string DocumentTemporaryDirectoryPath { get; init; } = DefaultDocumentTemporaryDirectoryPath();

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

    public static string DefaultDocumentTemporaryDirectoryPath()
    {
        var databaseDirectory = Path.GetDirectoryName(DefaultDatabasePath());
        if (string.IsNullOrWhiteSpace(databaseDirectory))
        {
            databaseDirectory = Path.Combine(Path.GetTempPath(), "FinanceAssistant");
        }

        return Path.Combine(databaseDirectory, "DocumentTemp");
    }
}
