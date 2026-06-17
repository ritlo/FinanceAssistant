using FinanceAssistant.Application.Assistant.Settings;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.Persistence.Documents;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Assistant;

public sealed class LiteDbAssistantSettingsRepository : IAssistantSettingsRepository
{
    private const string SettingsId = "assistant";

    private readonly LiteDbConnectionFactory connectionFactory;
    private readonly AssistantSettings defaultSettings;

    public LiteDbAssistantSettingsRepository(
        FinanceAssistantDataOptions options,
        AssistantSettings defaultSettings)
    {
        connectionFactory = new LiteDbConnectionFactory(options);
        this.defaultSettings = defaultSettings;
    }

    public AssistantSettings Get()
    {
        using var database = connectionFactory.Open();
        var document = Collection(database).FindById(SettingsId);

        return document?.ToSettings() ?? defaultSettings;
    }

    public Task SaveAsync(AssistantSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var database = connectionFactory.Open();
        Collection(database).Upsert(AssistantSettingsDocument.FromSettings(SettingsId, settings));

        return Task.CompletedTask;
    }

    private static ILiteCollection<AssistantSettingsDocument> Collection(LiteDatabase database)
    {
        return database.GetCollection<AssistantSettingsDocument>(
            LiteDbCollectionNames.AssistantSettings,
            BsonAutoId.Int32);
    }
}
