using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.ProcessMessage;
using FinanceAssistant.Web.Assistant;

namespace FinanceAssistant.Web.Tests.Assistant;

public sealed class AssistantToolPayloadDisplayModelTests
{
    [Fact]
    public void FromResultHidesReadPayloadWhenFriendlyMessageExists()
    {
        var payload = """{"note":"<script>alert('x')</script>"}""";
        var result = ProcessAssistantMessageResult.Success(
            "Parsed document summary.",
            "ReadParsedDocument",
            AssistantToolCallKind.Read,
            payload);

        var model = AssistantToolPayloadDisplayModel.FromResult(result);

        Assert.Null(model);
    }

    [Fact]
    public void FromResultKeepsWriteProposalPayloadAsInertText()
    {
        var payload = """{"serializedProposal":"<script>alert('x')</script>"}""";
        var result = ProcessAssistantMessageResult.Success(
            "Assistant proposal requires confirmation.",
            "ProposeTransaction",
            AssistantToolCallKind.WriteProposal,
            payload,
            Guid.NewGuid(),
            "fingerprint");

        var model = AssistantToolPayloadDisplayModel.FromResult(result);

        Assert.NotNull(model);
        Assert.Equal(payload, model.Text);
        Assert.Equal("ProposeTransaction", model.ToolName);
        Assert.Equal("Needs confirmation", model.KindLabel);
    }

    [Fact]
    public void FromResultReturnsNullWhenPayloadIsEmpty()
    {
        var result = ProcessAssistantMessageResult.Error("No model response was available.");

        var model = AssistantToolPayloadDisplayModel.FromResult(result);

        Assert.Null(model);
    }
}
