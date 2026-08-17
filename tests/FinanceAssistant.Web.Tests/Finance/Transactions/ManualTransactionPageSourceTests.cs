namespace FinanceAssistant.Web.Tests.Finance.Transactions;

public sealed class ManualTransactionPageSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    [Fact]
    public void ManualTransactionWorkflowHasDedicatedSidebarRoute()
    {
        var assistantPage = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Assistant.razor");
        var manualTransactionsPage = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/ManualTransactions.razor");
        var layout = ReadProjectFile("src/FinanceAssistant.Web/Components/Layout/MainLayout.razor");

        Assert.Contains("@page \"/\"", assistantPage, StringComparison.Ordinal);
        Assert.Contains("@page \"/assistant\"", assistantPage, StringComparison.Ordinal);
        Assert.Contains("@page \"/transactions/manual\"", manualTransactionsPage, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/\"", manualTransactionsPage, StringComparison.Ordinal);
        Assert.Contains("href=\"/transactions/manual\"", layout, StringComparison.Ordinal);
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
