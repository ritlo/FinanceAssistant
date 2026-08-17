using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Assistant.Confirmations;

public interface IAssistantConfirmationRepository
{
    Task AddAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default);

    Task<AssistantConfirmationRecord?> GetByTokenAsync(
        LocalProfileId profileId,
        Guid token,
        CancellationToken cancellationToken = default);

    Task<AssistantConfirmationRecord?> GetByFingerprintAsync(
        LocalProfileId profileId,
        string operationFingerprint,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default);

    Task<bool> TryClaimAsync(
        LocalProfileId profileId,
        Guid token,
        CancellationToken cancellationToken = default);
}
