using FinanceAssistant.Application.Assistant.Confirmations;

namespace FinanceAssistant.Web.Assistant.Confirmations;

public sealed record AssistantConfirmationPreviewModel(
    Guid Token,
    string OperationFingerprint,
    string ProposalType,
    string SerializedProposal,
    AssistantConfirmationStatus Status,
    DateTimeOffset ExpiresAt,
    string? CompletedResult)
{
    public static AssistantConfirmationPreviewModel FromResult(AssistantConfirmationResult result)
    {
        return new AssistantConfirmationPreviewModel(
            result.Token,
            result.OperationFingerprint,
            result.ProposalType,
            result.SerializedProposal,
            result.Status,
            result.ExpiresAt,
            result.CompletedResult);
    }
}
