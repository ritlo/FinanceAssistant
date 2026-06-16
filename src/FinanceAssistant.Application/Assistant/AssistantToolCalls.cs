using System.Text.Json;

namespace FinanceAssistant.Application.Assistant;

public enum AssistantToolCallKind
{
    Read,
    Advice,
    WriteProposal,
}

public abstract record AssistantToolCall(string Name, AssistantToolCallKind Kind);

public sealed record AssistantReadToolCall(
    string Name,
    JsonElement Parameters)
    : AssistantToolCall(Name, AssistantToolCallKind.Read);

public sealed record AssistantAdviceToolCall(
    AnalyzeSpendingPatternsRequest Request)
    : AssistantToolCall(AssistantToolNames.AnalyzeSpendingPatterns, AssistantToolCallKind.Advice);

public sealed record AssistantWriteProposalToolCall(
    string Name,
    object Proposal)
    : AssistantToolCall(Name, AssistantToolCallKind.WriteProposal);

public sealed record ProposeTransactionProposal(
    decimal Amount,
    string Description,
    DateOnly Date,
    string? TransactionType,
    string? CategoryName);

public sealed record ProposeNoteProposal(string Content);

public sealed record ProposePaymentReminderProposal(string Content, DateOnly DueDate);

public sealed record AnalyzeSpendingPatternsRequest(int Year, int Month);

public sealed record AnalyzeSpendingPatternsResult(
    int Year,
    int Month,
    bool HasSufficientData,
    IReadOnlyList<string> ObservedFacts,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> BudgetSuggestions,
    string? NoDataReason)
{
    public static AnalyzeSpendingPatternsResult NoData(int year, int month, string reason)
    {
        return new AnalyzeSpendingPatternsResult(
            year,
            month,
            HasSufficientData: false,
            ObservedFacts: [],
            Recommendations: [],
            BudgetSuggestions: [],
            reason);
    }
}

public sealed record AssistantModelParseResult(AssistantToolCall? ToolCall, string? ErrorMessage)
{
    public bool Succeeded => ToolCall is not null;

    public static AssistantModelParseResult Success(AssistantToolCall toolCall)
    {
        return new AssistantModelParseResult(toolCall, ErrorMessage: null);
    }

    public static AssistantModelParseResult Error(string message)
    {
        return new AssistantModelParseResult(ToolCall: null, message);
    }
}
