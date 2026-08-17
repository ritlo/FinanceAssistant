using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.ProcessMessage;
using FinanceAssistant.Web.Assistant;

namespace FinanceAssistant.Web.Tests.Assistant;

public sealed class AssistantProposalPreviewModelTests
{
    [Fact]
    public void FromResultExposesTokenAndFingerprintForPendingProposal()
    {
        var token = Guid.NewGuid();
        var result = ProcessAssistantMessageResult.Success(
            "Review this transaction before saving.",
            "ProposeTransaction",
            AssistantToolCallKind.WriteProposal,
            """{"amount":12.34}""",
            token,
            "fingerprint-123");

        var model = AssistantProposalPreviewModel.FromResult(result);

        Assert.NotNull(model);
        Assert.Equal(token, model.Token);
        Assert.Equal("fingerprint-123", model.OperationFingerprint);
        Assert.Equal("ProposeTransaction", model.ToolName);
        Assert.Equal("token " + token, model.TokenDisplay);
        Assert.Equal("fingerprint fingerprint-123", model.FingerprintDisplay);
    }

    [Fact]
    public void FromResultReturnsNullWhenAssistantResultDoesNotRequireConfirmation()
    {
        var result = ProcessAssistantMessageResult.Success(
            "Here are recent transactions.",
            "ReadTransactions",
            AssistantToolCallKind.Read,
            "[]");

        var model = AssistantProposalPreviewModel.FromResult(result);

        Assert.Null(model);
    }
}
