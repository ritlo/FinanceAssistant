namespace FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;

public sealed record CreateAssistantProposalRequest(
    string ProposalType,
    object Proposal);
