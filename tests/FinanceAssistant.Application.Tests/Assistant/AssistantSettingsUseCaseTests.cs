using FinanceAssistant.Application.Assistant.Settings;
using FinanceAssistant.Application.Assistant.Settings.GetAssistantSettings;
using FinanceAssistant.Application.Assistant.Settings.UpdateAssistantSettings;
using FinanceAssistant.Domain.Common;

namespace FinanceAssistant.Application.Tests.Assistant;

public sealed class AssistantSettingsUseCaseTests
{
    [Fact]
    public void GetSettingsReturnsPersistedSettings()
    {
        var repository = new FakeAssistantSettingsRepository(new AssistantSettings(
            WriteProposalsEnabled: false,
            EndpointUrl: "https://models.example.test/v1/chat/completions",
            EndpointPort: 443,
            AllowRemoteEndpoint: true));
        var useCase = new GetAssistantSettingsUseCase(repository);

        var settings = useCase.Execute();

        Assert.False(settings.WriteProposalsEnabled);
        Assert.Equal("https://models.example.test/v1/chat/completions", settings.EndpointUrl);
        Assert.Equal(443, settings.EndpointPort);
        Assert.True(settings.AllowRemoteEndpoint);
    }

    [Fact]
    public async Task UpdateSettingsNormalizesEndpointUrlAndPersistsPortSeparately()
    {
        var repository = new FakeAssistantSettingsRepository(AssistantSettings.Default);
        var useCase = new UpdateAssistantSettingsUseCase(repository);

        var settings = await useCase.ExecuteAsync(new UpdateAssistantSettingsRequest(
            WriteProposalsEnabled: true,
            EndpointUrl: "https://models.example.test:9443/v1/chat/completions",
            EndpointPort: 8443,
            AllowRemoteEndpoint: true));

        Assert.Equal("https://models.example.test/v1/chat/completions", settings.EndpointUrl);
        Assert.Equal(8443, settings.EndpointPort);
        Assert.True(settings.AllowRemoteEndpoint);
        Assert.Equal(settings, repository.Get());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public async Task UpdateSettingsRejectsInvalidPorts(int port)
    {
        var useCase = new UpdateAssistantSettingsUseCase(new FakeAssistantSettingsRepository(AssistantSettings.Default));

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            new UpdateAssistantSettingsRequest(
                WriteProposalsEnabled: true,
                EndpointUrl: AssistantSettings.DefaultEndpointUrl,
                EndpointPort: port,
                AllowRemoteEndpoint: false)));

        Assert.Equal("Assistant endpoint port must be between 1 and 65535.", exception.Message);
    }

    [Fact]
    public async Task UpdateSettingsRejectsNonHttpEndpointUrl()
    {
        var useCase = new UpdateAssistantSettingsUseCase(new FakeAssistantSettingsRepository(AssistantSettings.Default));

        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(
            new UpdateAssistantSettingsRequest(
                WriteProposalsEnabled: true,
                EndpointUrl: "file:///tmp/model.sock",
                EndpointPort: 8080,
                AllowRemoteEndpoint: false)));

        Assert.Equal("Assistant endpoint URL must be an absolute HTTP or HTTPS URL.", exception.Message);
    }

    private sealed class FakeAssistantSettingsRepository : IAssistantSettingsRepository
    {
        private AssistantSettings settings;

        public FakeAssistantSettingsRepository(AssistantSettings settings)
        {
            this.settings = settings;
        }

        public AssistantSettings Get()
        {
            return settings;
        }

        public Task SaveAsync(AssistantSettings settings, CancellationToken cancellationToken = default)
        {
            this.settings = settings;
            return Task.CompletedTask;
        }
    }
}
