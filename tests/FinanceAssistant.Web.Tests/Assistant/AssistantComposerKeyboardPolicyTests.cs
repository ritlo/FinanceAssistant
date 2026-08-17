using FinanceAssistant.Web.Assistant;

namespace FinanceAssistant.Web.Tests.Assistant;

public sealed class AssistantComposerKeyboardPolicyTests
{
    [Fact]
    public void SubmitKeyMatchesComposerScriptDefault()
    {
        Assert.Equal("Enter", AssistantComposerKeyboardPolicy.SubmitKey);
    }

    [Fact]
    public void SendButtonSelectorTargetsPrimaryButton()
    {
        Assert.Equal("button.primary-button", AssistantComposerKeyboardPolicy.SendButtonSelector);
    }

    [Theory]
    [InlineData("Enter", false, true)]
    [InlineData("Enter", true, false)]
    [InlineData("a", false, false)]
    public void ShouldSubmitOnlyForEnterWithoutShift(string key, bool shiftKey, bool expected)
    {
        var shouldSubmit = AssistantComposerKeyboardPolicy.ShouldSubmit(key, shiftKey);

        Assert.Equal(expected, shouldSubmit);
    }
}
