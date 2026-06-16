using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Assistant;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Assistant;

[Collection("Sequential")]
public sealed class LiteDbAssistantConfirmationRepositoryTests
{
    [Fact]
    public async Task ConfirmationPersists()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbAssistantConfirmationRepository(options);
        var record = CreateRecord(profileId);

        await repository.AddAsync(record);

        var persisted = await repository.GetByTokenAsync(profileId, record.Token);
        Assert.NotNull(persisted);
        Assert.Equal(record.OperationFingerprint, persisted.OperationFingerprint);
        Assert.Equal(AssistantConfirmationStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task CompletedResultSurvivesRepositoryRestart()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var record = CreateRecord(profileId);
        record.MarkClaimed();
        record.MarkCompleted("""{"id":"result"}""");
        await new LiteDbAssistantConfirmationRepository(options).AddAsync(record);

        var restarted = new LiteDbAssistantConfirmationRepository(options);
        var persisted = await restarted.GetByTokenAsync(profileId, record.Token);

        Assert.NotNull(persisted);
        Assert.Equal(AssistantConfirmationStatus.Completed, persisted.Status);
        Assert.Equal("""{"id":"result"}""", persisted.CompletedResult);
    }

    [Fact]
    public async Task AtomicClaimPreventsDuplicateClaim()
    {
        using var directory = TemporaryDirectory.Create();
        var options = CreateOptions(directory);
        var profileId = new LiteDbSchemaInitializer(options).Initialize();
        var repository = new LiteDbAssistantConfirmationRepository(options);
        var record = CreateRecord(profileId);
        await repository.AddAsync(record);

        var first = await repository.TryClaimAsync(profileId, record.Token);
        var second = await repository.TryClaimAsync(profileId, record.Token);

        var persisted = await repository.GetByTokenAsync(profileId, record.Token);
        Assert.True(first);
        Assert.False(second);
        Assert.Equal(AssistantConfirmationStatus.Claimed, persisted!.Status);
    }

    private static AssistantConfirmationRecord CreateRecord(LocalProfileId profileId)
    {
        return AssistantConfirmationRecord.Create(
            profileId,
            "fingerprint",
            AssistantToolNames.ProposeNote,
            """{"content":"test"}""",
            new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
            TimeSpan.FromMinutes(10));
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
