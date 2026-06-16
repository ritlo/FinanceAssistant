using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Application.Assistant.Confirmations.CancelAssistantProposal;
using FinanceAssistant.Application.Assistant.Confirmations.ConfirmAssistantProposal;
using FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;
using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Notes;
using FinanceAssistant.Application.PersonalRecords.Notes.CreateNote;
using FinanceAssistant.Application.PersonalRecords.Reminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.Tests.Assistant;

public sealed class AssistantConfirmationUseCaseTests
{
    [Fact]
    public async Task CreateProposalCreatesPendingConfirmation()
    {
        var fixture = new Fixture();

        var result = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Remember this")));

        Assert.Equal(AssistantConfirmationStatus.Pending, result.Status);
        Assert.Equal(AssistantToolNames.ProposeNote, result.ProposalType);
        Assert.NotEqual(Guid.Empty, result.Token);
        Assert.Equal(fixture.Now.AddMinutes(10), result.ExpiresAt);
        Assert.Single(fixture.Confirmations.Records);
    }

    [Fact]
    public async Task ConfirmationInvokesNormalUseCaseAndStoresResult()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Confirmed note")));

        var completed = await fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
            pending.Token,
            pending.OperationFingerprint));

        Assert.Equal(AssistantConfirmationStatus.Completed, completed.Status);
        Assert.Contains("Confirmed note", completed.CompletedResult, StringComparison.Ordinal);
        var note = Assert.Single(fixture.Notes.Notes);
        Assert.Equal(fixture.ProfileId, note.ProfileId);
    }

    [Fact]
    public async Task ConfirmationUsesServerProfile()
    {
        var fixture = new Fixture();
        var otherProfile = LocalProfileId.New();
        var record = AssistantConfirmationRecord.Create(
            otherProfile,
            "abc",
            AssistantToolNames.ProposeNote,
            """{"content":"Other"}""",
            fixture.Now,
            TimeSpan.FromMinutes(10));
        await fixture.Confirmations.AddAsync(record);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(record.Token, "abc")));

        Assert.Equal("Assistant confirmation was not found.", exception.Message);
        Assert.Empty(fixture.Notes.Notes);
    }

    [Fact]
    public async Task ExpiredTokenIsRejected()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Too late")));
        fixture.Clock.UtcNow = fixture.Now.AddMinutes(11);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
                pending.Token,
                pending.OperationFingerprint)));

        Assert.Equal("Assistant confirmation expired.", exception.Message);
        Assert.Empty(fixture.Notes.Notes);
    }

    [Fact]
    public async Task CancelledTokenIsRejected()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Cancelled")));
        await fixture.Cancel.ExecuteAsync(new CancelAssistantProposalRequest(pending.Token));

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
                pending.Token,
                pending.OperationFingerprint)));

        Assert.Equal("Assistant confirmation was cancelled.", exception.Message);
        Assert.Empty(fixture.Notes.Notes);
    }

    [Fact]
    public async Task MalformedProposalIsRejectedWithoutSideEffects()
    {
        var fixture = new Fixture();
        var record = AssistantConfirmationRecord.Create(
            fixture.ProfileId,
            "bad-fingerprint",
            AssistantToolNames.ProposeNote,
            """{"wrong":"shape"}""",
            fixture.Now,
            TimeSpan.FromMinutes(10));
        await fixture.Confirmations.AddAsync(record);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(record.Token, "bad-fingerprint")));

        Assert.Empty(fixture.Notes.Notes);
    }

    [Fact]
    public async Task RepeatedConfirmationReturnsStoredResultWithoutDuplicateWrite()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Once")));

        var first = await fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
            pending.Token,
            pending.OperationFingerprint));
        var second = await fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
            pending.Token,
            pending.OperationFingerprint));

        Assert.Equal(first.CompletedResult, second.CompletedResult);
        Assert.Single(fixture.Notes.Notes);
    }

    [Fact]
    public async Task ConcurrentConfirmationDoesNotDuplicateWrite()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreateProposal.ExecuteAsync(new CreateAssistantProposalRequest(
            AssistantToolNames.ProposeNote,
            new ProposeNoteProposal("Concurrent")));

        var first = await fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
            pending.Token,
            pending.OperationFingerprint));
        var second = await fixture.Confirm.ExecuteAsync(new ConfirmAssistantProposalRequest(
            pending.Token,
            pending.OperationFingerprint));

        Assert.Equal(AssistantConfirmationStatus.Completed, first.Status);
        Assert.Equal(AssistantConfirmationStatus.Completed, second.Status);
        Assert.Equal(1, fixture.Confirmations.ClaimCount);
        Assert.Single(fixture.Notes.Notes);
    }

    private sealed class Fixture
    {
        public LocalProfileId ProfileId { get; } = LocalProfileId.New();
        public DateTimeOffset Now { get; } = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        public FakeAssistantConfirmationRepository Confirmations { get; } = new();
        public FakeNoteRepository Notes { get; } = new();
        public FakeReminderRepository Reminders { get; } = new();
        public FakeCategoryRepository Categories { get; } = new();
        public FakeTransactionRepository Transactions { get; } = new();
        public MutableClock Clock { get; }
        public CreateAssistantProposalUseCase CreateProposal { get; }
        public ConfirmAssistantProposalUseCase Confirm { get; }
        public CancelAssistantProposalUseCase Cancel { get; }

        public Fixture()
        {
            Clock = new MutableClock(Now);
            Categories.AddExisting(Category.Create(ProfileId, "Other", TransactionType.Expense));
            var currentProfile = new FixedCurrentProfileProvider(ProfileId);
            var logTransaction = new LogTransactionUseCase(
                currentProfile,
                Categories,
                Transactions,
                new FakeTransactionChangeNotifier());
            var createNote = new CreateNoteUseCase(currentProfile, Notes, Clock);
            var createReminder = new CreateReminderUseCase(currentProfile, Reminders, Clock);

            CreateProposal = new CreateAssistantProposalUseCase(currentProfile, Confirmations, Clock);
            Confirm = new ConfirmAssistantProposalUseCase(
                currentProfile,
                Confirmations,
                logTransaction,
                createNote,
                createReminder,
                Clock);
            Cancel = new CancelAssistantProposalUseCase(currentProfile, Confirmations, Clock);
        }
    }

    private sealed class FixedCurrentProfileProvider : ICurrentProfileProvider
    {
        private readonly LocalProfileId profileId;

        public FixedCurrentProfileProvider(LocalProfileId profileId)
        {
            this.profileId = profileId;
        }

        public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(profileId);
        }
    }

    private sealed class MutableClock : IClock
    {
        public MutableClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class FakeAssistantConfirmationRepository : IAssistantConfirmationRepository
    {
        private readonly object gate = new();

        public List<AssistantConfirmationRecord> Records { get; } = [];
        public int ClaimCount { get; private set; }

        public Task AddAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<AssistantConfirmationRecord?> GetByTokenAsync(
            LocalProfileId profileId,
            Guid token,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AssistantConfirmationRecord?>(
                Records.SingleOrDefault(record => record.ProfileId == profileId && record.Token == token));
        }

        public Task<AssistantConfirmationRecord?> GetByFingerprintAsync(
            LocalProfileId profileId,
            string operationFingerprint,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AssistantConfirmationRecord?>(
                Records.SingleOrDefault(record =>
                    record.ProfileId == profileId && record.OperationFingerprint == operationFingerprint));
        }

        public Task UpdateAsync(AssistantConfirmationRecord record, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> TryClaimAsync(
            LocalProfileId profileId,
            Guid token,
            CancellationToken cancellationToken = default)
        {
            lock (gate)
            {
                var record = Records.SingleOrDefault(candidate =>
                    candidate.ProfileId == profileId && candidate.Token == token);
                if (record?.Status != AssistantConfirmationStatus.Pending)
                {
                    return Task.FromResult(false);
                }

                record.MarkClaimed();
                ClaimCount++;
                return Task.FromResult(true);
            }
        }
    }

    private sealed class FakeNoteRepository : INoteRepository
    {
        public List<Note> Notes { get; } = [];

        public Task AddNoteAsync(Note note, CancellationToken cancellationToken = default)
        {
            Notes.Add(note);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Note>> ListNotesAsync(LocalProfileId profileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Note>>(Notes.Where(note => note.ProfileId == profileId).ToArray());
        }

        public Task<Note?> GetNoteAsync(LocalProfileId profileId, NoteId noteId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Note?>(Notes.SingleOrDefault(note => note.ProfileId == profileId && note.Id == noteId));
        }

        public Task DeleteNoteAsync(LocalProfileId profileId, NoteId noteId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReminderRepository : IReminderRepository
    {
        public List<PaymentReminder> Reminders { get; } = [];

        public Task AddReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
        {
            Reminders.Add(reminder);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PaymentReminder>> ListRemindersAsync(LocalProfileId profileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PaymentReminder>>(Reminders.Where(reminder => reminder.ProfileId == profileId).ToArray());
        }

        public Task<PaymentReminder?> GetReminderAsync(LocalProfileId profileId, PaymentReminderId reminderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaymentReminder?>(Reminders.SingleOrDefault(reminder => reminder.ProfileId == profileId && reminder.Id == reminderId));
        }

        public Task UpdateReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteReminderAsync(LocalProfileId profileId, PaymentReminderId reminderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> categories = [];

        public void AddExisting(Category category)
        {
            categories.Add(category);
        }

        public Task<IReadOnlyList<Category>> ListCategoriesAsync(LocalProfileId profileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(categories.Where(category => category.ProfileId == profileId).ToArray());
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<CategorizationRule>> ListCategorizationRulesAsync(
            LocalProfileId profileId,
            TransactionType transactionType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategorizationRule>>([]);
        }

        public Task AddCategorizationRuleAsync(CategorizationRule rule, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Transactions { get; } = [];

        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(LocalProfileId profileId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(Transactions.Where(transaction => transaction.ProfileId == profileId).ToArray());
        }

        public Task<Transaction?> GetTransactionAsync(LocalProfileId profileId, TransactionId transactionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Transaction?>(Transactions.SingleOrDefault(transaction => transaction.ProfileId == profileId && transaction.Id == transactionId));
        }

        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteTransactionAsync(LocalProfileId profileId, TransactionId transactionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeTransactionChangeNotifier : ITransactionChangeNotifier
    {
        public Task PublishTransactionChangedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
