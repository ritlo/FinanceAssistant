using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class MetadataDocument
{
    [BsonId]
    public string Id { get; set; } = "financeassistant";

    public int SchemaVersion { get; set; }

    public string Currency { get; set; } = string.Empty;
}
