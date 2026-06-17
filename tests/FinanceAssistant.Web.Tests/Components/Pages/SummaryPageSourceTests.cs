namespace FinanceAssistant.Web.Tests.Components.Pages;

public sealed class SummaryPageSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    [Fact]
    public void SummaryPageDefinesDedicatedInteractiveRouteAndUsesDashboardState()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Summary.razor");

        Assert.Contains("@page \"/summary\"", source, StringComparison.Ordinal);
        Assert.Contains("@rendermode InteractiveServer", source, StringComparison.Ordinal);
        Assert.Contains("@inject TransactionDashboardState DashboardState", source, StringComparison.Ordinal);
        Assert.Contains("await DashboardState.InitializeAsync()", source, StringComparison.Ordinal);
        Assert.Contains("DashboardState.SetSummaryPeriodAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryPageUsesLiveStateInsteadOfMockScriptData()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Summary.razor");

        Assert.Contains("MonthlySummaryPanel", source, StringComparison.Ordinal);
        Assert.Contains("DashboardState.Transactions", source, StringComparison.Ordinal);
        Assert.Contains("DashboardState.Summary", source, StringComparison.Ordinal);
        Assert.DoesNotContain("monthData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("switchMonth", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryPanelComponentDefinesAnimatedCategoryBars()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Finance/Summaries/MonthlySummaryPanel.razor");
        var css = ReadProjectFile("src/FinanceAssistant.Web/wwwroot/app.css");

        Assert.Contains("class=\"panel monthly-summary-panel\"", source, StringComparison.Ordinal);
        Assert.Contains("summary-category-fill", source, StringComparison.Ordinal);
        Assert.Contains("--target-width", source, StringComparison.Ordinal);
        Assert.Contains("@key=\"SummaryKey\"", source, StringComparison.Ordinal);
        Assert.Contains("@keyframes monthly-summary-fill", css, StringComparison.Ordinal);
        Assert.Contains("animation: monthly-summary-fill 1s", css, StringComparison.Ordinal);
    }

    [Fact]
    public void AssistantPageIsNotUsedAsSummaryRoute()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Assistant.razor");

        Assert.DoesNotContain("@page \"/summary\"", source, StringComparison.Ordinal);
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
