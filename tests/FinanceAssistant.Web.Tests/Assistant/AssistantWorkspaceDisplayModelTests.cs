using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Web.Assistant;

namespace FinanceAssistant.Web.Tests.Assistant;

public sealed class AssistantWorkspaceDisplayModelTests
{
    [Fact]
    public void FromDisclosureLabelsLocalModelAsLocalFirst()
    {
        var disclosure = new AssistantConfigurationDisclosure(
            new Uri("http://localhost:11434/v1/chat/completions"),
            "local",
            IsRemoteEndpoint: false,
            IsRemoteAllowed: false,
            RequiresRemoteDisclosure: false,
            WarningMessage: null);

        var model = AssistantWorkspaceDisplayModel.FromDisclosure(disclosure);

        Assert.Equal("http://localhost:11434/v1/chat/completions", model.Endpoint);
        Assert.Equal("local", model.Model);
        Assert.Equal("Local", model.LocationLabel);
        Assert.Equal("off", model.RemoteLabel);
        Assert.Equal("Local-first", model.StatusPill);
        Assert.False(model.RequiresRemoteDisclosure);
        Assert.Null(model.WarningMessage);
    }

    [Fact]
    public void FromDisclosureLabelsRemoteModelWithDisclosureWarning()
    {
        var disclosure = new AssistantConfigurationDisclosure(
            new Uri("https://models.example.test/v1/chat/completions"),
            "remote-model",
            IsRemoteEndpoint: true,
            IsRemoteAllowed: true,
            RequiresRemoteDisclosure: true,
            WarningMessage: "Financial data may leave this machine.");

        var model = AssistantWorkspaceDisplayModel.FromDisclosure(disclosure);

        Assert.Equal("Remote", model.LocationLabel);
        Assert.Equal("on", model.RemoteLabel);
        Assert.Equal("Remote disclosure", model.StatusPill);
        Assert.True(model.RequiresRemoteDisclosure);
        Assert.Equal("Financial data may leave this machine.", model.WarningMessage);
    }

    [Fact]
    public void ToolSummariesSeparateImmediateReadsFromConfirmedWrites()
    {
        Assert.Contains("ReadTransactions", AssistantWorkspaceDisplayModel.ImmediateReadTools);
        Assert.Contains("GetMonthlySummary", AssistantWorkspaceDisplayModel.ImmediateReadTools);
        Assert.Contains("AnalyzeSpendingPatterns", AssistantWorkspaceDisplayModel.ImmediateReadTools);
        Assert.Contains("ReadParsedDocument", AssistantWorkspaceDisplayModel.ImmediateReadTools);

        Assert.Contains("ProposeTransaction", AssistantWorkspaceDisplayModel.ConfirmedWriteTools);
        Assert.Contains("ProposeNote", AssistantWorkspaceDisplayModel.ConfirmedWriteTools);
        Assert.Contains("ProposePaymentReminder", AssistantWorkspaceDisplayModel.ConfirmedWriteTools);
    }

    [Fact]
    public void QuickPromptsCoverFinanceDocumentsAndReminderWorkflows()
    {
        Assert.Contains("Explain this month", AssistantWorkspaceDisplayModel.QuickPrompts);
        Assert.Contains("Find unusual spending", AssistantWorkspaceDisplayModel.QuickPrompts);
        Assert.Contains("Draft a reminder", AssistantWorkspaceDisplayModel.QuickPrompts);
        Assert.Contains("Read parsed documents", AssistantWorkspaceDisplayModel.QuickPrompts);
    }
}
