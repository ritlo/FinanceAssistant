using FinanceAssistant.Application.Assistant.Settings;
using FinanceAssistant.Infrastructure.Assistant;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Assistant;

[Collection("Sequential")]
public sealed class LiteDbAssistantSettingsRepositoryTests
{
    [Fact]
    public void GetReturnsDefaultSettingsWhenNoSettingsHaveBeenSaved()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        new LiteDbSchemaInitializer(options).Initialize();
        var defaults = AssistantSettings.Default with
        {
            EndpointPort = 11434,
        };
        var repository = new LiteDbAssistantSettingsRepository(options, defaults);

        var settings = repository.Get();

        Assert.Equal(defaults, settings);
    }

    [Fact]
    public async Task SavedSettingsSurviveRepositoryRestart()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbAssistantSettingsRepository(options, AssistantSettings.Default);
        var changed = new AssistantSettings(
            WriteProposalsEnabled: false,
            EndpointUrl: "https://models.example.test/v1/chat/completions",
            EndpointPort: 9443,
            AllowRemoteEndpoint: true);

        await repository.SaveAsync(changed);

        var restarted = new LiteDbAssistantSettingsRepository(options, AssistantSettings.Default);
        Assert.Equal(changed, restarted.Get());
    }

    private static FinanceAssistantDataOptions CreateOptions(TemporaryDirectory directory)
    {
        return new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            DocumentTemporaryDirectoryPath = Path.Combine(directory.Path, "document-temp"),
            Currency = "USD",
        };
    }
}
