namespace FinanceAssistant.Web.Assistant;

public static class AssistantComposerKeyboardPolicy
{
    public const string SubmitKey = "Enter";

    public static bool ShouldSubmit(string key, bool shiftKey)
    {
        return string.Equals(key, SubmitKey, StringComparison.Ordinal) && !shiftKey;
    }
}
