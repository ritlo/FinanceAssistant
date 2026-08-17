namespace FinanceAssistant.Web.Tests.Components.Pages;

public sealed class SupportingPageSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);

    public static TheoryData<string, string[]> SupportingPages => new()
    {
        {
            "src/FinanceAssistant.Web/Components/Pages/ManualTransactions.razor",
            [
                "<main class=\"supporting-page transaction-shell\">",
                "<section class=\"panel transaction-entry\">",
                "<section class=\"panel monthly-summary\">",
                "<section class=\"panel transaction-list\">",
                "class=\"list-row transaction-row\""
            ]
        },
        {
            "src/FinanceAssistant.Web/Components/Pages/Documents.razor",
            [
                "<main class=\"supporting-page documents-shell\">",
                "<section class=\"panel documents-entry\">",
                "<section class=\"panel documents-list\">",
                "class=\"list-row document-row\""
            ]
        },
        {
            "src/FinanceAssistant.Web/Components/Pages/Notes.razor",
            [
                "<main class=\"supporting-page notes-shell\">",
                "<section class=\"panel notes-entry\">",
                "<section class=\"panel notes-list\">",
                "class=\"list-row note-row\""
            ]
        },
        {
            "src/FinanceAssistant.Web/Components/Pages/Payments.razor",
            [
                "<main class=\"supporting-page payments-shell\">",
                "<section class=\"panel payments-entry\">",
                "<section class=\"panel payments-list\">",
                "class=\"list-row payment-row\""
            ]
        },
        {
            "src/FinanceAssistant.Web/Components/Pages/AssistantConfirmations.razor",
            [
                "<main class=\"supporting-page assistant-shell\">",
                "<section class=\"panel assistant-status\">",
                "class=\"assistant-confirmation-form panel-body\"",
                "class=\"list-row assistant-confirmation-preview\""
            ]
        }
    };

    [Theory]
    [MemberData(nameof(SupportingPages))]
    public void SupportingPagesUseDarkPanelHierarchy(string relativePath, string[] requiredFragments)
    {
        var source = ReadProjectFile(relativePath);

        foreach (var fragment in requiredFragments)
        {
            Assert.Contains(fragment, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SupportingPageCssDefinesMobileSafeDarkPanels()
    {
        var css = ReadProjectFile("src/FinanceAssistant.Web/wwwroot/app.css");

        Assert.Contains(".supporting-page", css, StringComparison.Ordinal);
        Assert.Contains(".panel-body", css, StringComparison.Ordinal);
        Assert.Contains(".list-row", css, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 760px)", css, StringComparison.Ordinal);
        Assert.Contains(".supporting-page", css[css.IndexOf("@media (max-width: 760px)", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentsPagePreservesUploadSecurityMessagesAndInertPreview()
    {
        var source = ReadProjectFile("src/FinanceAssistant.Web/Components/Pages/Documents.razor");

        Assert.Contains("accept=\"application/pdf,text/plain\"", source, StringComparison.Ordinal);
        Assert.Contains("OpenReadStream(UploadedDocument.MaximumByteLength)", source, StringComparison.Ordinal);
        Assert.Contains("Document exceeds the 10 MiB size limit.", source, StringComparison.Ordinal);
        Assert.Contains("<pre>@Preview(parsed.UntrustedExtractedText)</pre>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MarkupString", source, StringComparison.Ordinal);
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
