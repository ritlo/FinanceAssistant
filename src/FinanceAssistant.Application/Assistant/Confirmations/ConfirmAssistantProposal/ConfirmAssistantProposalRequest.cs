namespace FinanceAssistant.Application.Assistant.Confirmations.ConfirmAssistantProposal;

public sealed record ConfirmAssistantProposalRequest(
    Guid Token,
    string OperationFingerprint);
