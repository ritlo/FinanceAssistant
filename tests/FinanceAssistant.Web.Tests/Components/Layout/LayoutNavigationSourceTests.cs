namespace FinanceAssistant.Web.Tests.Components.Layout;

public sealed class LayoutNavigationSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    public static TheoryData<string, string> RequiredNavigationItems => new()
    {
        { "href=\"/assistant\"", "Assistant" },
        { "href=\"/assistant#monthly-summary\"", "Summary" },
        { "href=\"/transactions/manual\"", "Manual transaction" },
        { "href=\"/documents\"", "Documents" },
        { "href=\"/notes\"", "Notes" },
        { "href=\"/payments\"", "Reminders" },
        { "href=\"/assistant/confirmations\"", "Confirmations" },
    };

    [Theory]
    [MemberData(nameof(RequiredNavigationItems))]
    public void SidebarExposesMockupNavigationDestinations(string hrefFragment, string label)
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Layout/MainLayout.razor");

        Assert.Contains(hrefFragment, source, StringComparison.Ordinal);
        Assert.Contains($"<span>{label}</span>", source, StringComparison.Ordinal);
    }

    private static string ReadProjectFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinanceAssistant.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
