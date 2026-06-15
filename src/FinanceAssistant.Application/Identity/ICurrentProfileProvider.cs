using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Identity;

public interface ICurrentProfileProvider
{
    ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default);
}
