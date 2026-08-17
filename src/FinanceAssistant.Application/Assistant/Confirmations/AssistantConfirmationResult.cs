namespace FinanceAssistant.Application.Assistant.Confirmations;

public sealed record AssistantConfirmationResult(
    Guid Token,
    string OperationFingerprint,
    string ProposalType,
    string SerializedProposal,
    AssistantConfirmationStatus Status,
    DateTimeOffset ExpiresAt,
    string? CompletedResult)
{
    public static AssistantConfirmationResult FromRecord(AssistantConfirmationRecord record)
    {
        return new AssistantConfirmationResult(
            record.Token,
            record.OperationFingerprint,
            record.ProposalType,
            record.SerializedProposal,
            record.Status,
            record.ExpiresAt,
            record.CompletedResult);
    }
}
