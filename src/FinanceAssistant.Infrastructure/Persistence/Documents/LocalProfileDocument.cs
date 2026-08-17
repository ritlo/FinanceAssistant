using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class LocalProfileDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
