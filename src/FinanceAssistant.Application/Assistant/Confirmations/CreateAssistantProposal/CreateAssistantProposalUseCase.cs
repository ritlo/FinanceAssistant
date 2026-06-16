using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;

namespace FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;

public sealed class CreateAssistantProposalUseCase
{
    private static readonly TimeSpan ConfirmationTimeToLive = TimeSpan.FromMinutes(10);

    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IAssistantConfirmationRepository repository;
    private readonly IClock clock;

    public CreateAssistantProposalUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IAssistantConfirmationRepository repository,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<AssistantConfirmationResult> ExecuteAsync(
        CreateAssistantProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var serializedProposal = AssistantProposalSerializer.Serialize(request.Proposal);
        var fingerprint = AssistantProposalSerializer.Fingerprint(request.ProposalType, serializedProposal);
        var existing = await repository.GetByFingerprintAsync(profileId, fingerprint, cancellationToken);

        if (existing is not null && existing.Status is not AssistantConfirmationStatus.Cancelled and not AssistantConfirmationStatus.Expired)
        {
            return AssistantConfirmationResult.FromRecord(existing);
        }

        var record = AssistantConfirmationRecord.Create(
            profileId,
            fingerprint,
            request.ProposalType,
            serializedProposal,
            clock.UtcNow,
            ConfirmationTimeToLive);

        await repository.AddAsync(record, cancellationToken);

        return AssistantConfirmationResult.FromRecord(record);
    }
}
