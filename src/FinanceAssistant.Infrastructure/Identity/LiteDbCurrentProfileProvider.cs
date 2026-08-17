using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;

namespace FinanceAssistant.Infrastructure.Identity;

public sealed class LiteDbCurrentProfileProvider : ICurrentProfileProvider
{
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbCurrentProfileProvider(FinanceAssistantDataOptions options)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        var profile = database
            .GetCollection<LocalProfileDocument>(LiteDbCollectionNames.LocalProfiles, LiteDB.BsonAutoId.Guid)
            .FindAll()
            .Single();

        return ValueTask.FromResult(new LocalProfileId(profile.Id));
    }
}
