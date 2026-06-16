using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Application.Assistant.Confirmations.CancelAssistantProposal;

public sealed class CancelAssistantProposalUseCase
{
    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IAssistantConfirmationRepository repository;
    private readonly IClock clock;

    public CancelAssistantProposalUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IAssistantConfirmationRepository repository,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.repository = repository;
        this.clock = clock;
    }

    public async Task<AssistantConfirmationResult> ExecuteAsync(
        CancelAssistantProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var record = await repository.GetByTokenAsync(profileId, request.Token, cancellationToken);

        if (record is null)
        {
            throw new DomainValidationException("Assistant confirmation was not found.");
        }

        if (record.ExpiresAt <= clock.UtcNow)
        {
            record.MarkExpired();
        }
        else
        {
            record.MarkCancelled();
        }

        await repository.UpdateAsync(record, cancellationToken);

        return AssistantConfirmationResult.FromRecord(record);
    }
}
