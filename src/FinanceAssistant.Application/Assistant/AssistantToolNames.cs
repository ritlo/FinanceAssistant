namespace FinanceAssistant.Application.Assistant;

public static class AssistantToolNames
{
    public const string ReadTransactions = nameof(ReadTransactions);
    public const string GetMonthlySummary = nameof(GetMonthlySummary);
    public const string AnalyzeSpendingPatterns = nameof(AnalyzeSpendingPatterns);
    public const string GetNotes = nameof(GetNotes);
    public const string GetPaymentReminders = nameof(GetPaymentReminders);
    public const string ProposeTransaction = nameof(ProposeTransaction);
    public const string ProposeNote = nameof(ProposeNote);
    public const string ProposePaymentReminder = nameof(ProposePaymentReminder);
    public const string ReadParsedDocument = nameof(ReadParsedDocument);

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ReadTransactions,
        GetMonthlySummary,
        AnalyzeSpendingPatterns,
        GetNotes,
        GetPaymentReminders,
        ProposeTransaction,
        ProposeNote,
        ProposePaymentReminder,
        ReadParsedDocument,
    };

    public static bool IsWriteProposal(string toolName)
    {
        return string.Equals(toolName, ProposeTransaction, StringComparison.Ordinal)
            || string.Equals(toolName, ProposeNote, StringComparison.Ordinal)
            || string.Equals(toolName, ProposePaymentReminder, StringComparison.Ordinal);
    }
}
