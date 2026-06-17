namespace FinanceAssistant.Web.Tests.Components.Pages;

public sealed class AssistantPageSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    [Fact]
    public void AssistantPageRendersToolPayloadsAsInertText()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Assistant.razor");

        Assert.Contains("AssistantToolPayloadDisplayModel.FromResult", source, StringComparison.Ordinal);
        Assert.Contains("<pre>@payload.Text</pre>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MarkupString", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AssistantPageRendersPendingProposalTokenAndFingerprintDisplayFields()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Assistant.razor");

        Assert.Contains("AssistantProposalPreviewModel.FromResult", source, StringComparison.Ordinal);
        Assert.Contains("<code>@proposal.TokenDisplay</code>", source, StringComparison.Ordinal);
        Assert.Contains("<code>@proposal.FingerprintDisplay</code>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WebTestsDoNotUseLiveBrowserOrComponentPackages()
    {
        var project = ReadProjectFile("tests/FinanceAssistant.Web.Tests/FinanceAssistant.Web.Tests.csproj");

        Assert.DoesNotContain("bunit", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Playwright", project, StringComparison.OrdinalIgnoreCase);
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
