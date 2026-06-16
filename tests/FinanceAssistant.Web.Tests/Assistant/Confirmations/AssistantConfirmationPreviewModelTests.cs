using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Web.Assistant.Confirmations;

namespace FinanceAssistant.Web.Tests.Assistant.Confirmations;

public sealed class AssistantConfirmationPreviewModelTests
{
    [Fact]
    public void FromResultMapsPreviewFields()
    {
        var result = new AssistantConfirmationResult(
            Guid.NewGuid(),
            "fingerprint",
            "ProposeNote",
            """{"content":"hello"}""",
            AssistantConfirmationStatus.Pending,
            new DateTimeOffset(2026, 6, 16, 12, 10, 0, TimeSpan.Zero),
            CompletedResult: null);

        var model = AssistantConfirmationPreviewModel.FromResult(result);

        Assert.Equal(result.Token, model.Token);
        Assert.Equal("fingerprint", model.OperationFingerprint);
        Assert.Equal("ProposeNote", model.ProposalType);
        Assert.Equal("""{"content":"hello"}""", model.SerializedProposal);
        Assert.Equal(AssistantConfirmationStatus.Pending, model.Status);
        Assert.Equal(result.ExpiresAt, model.ExpiresAt);
        Assert.Null(model.CompletedResult);
    }
}
