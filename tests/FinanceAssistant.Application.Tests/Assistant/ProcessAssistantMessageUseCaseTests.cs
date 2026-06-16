using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;
using FinanceAssistant.Application.Assistant.ProcessMessage;
using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Application.Documents.GetParsedDocument;
using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Notes;
using FinanceAssistant.Application.PersonalRecords.Notes.ListNotes;
using FinanceAssistant.Application.PersonalRecords.Reminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.ListReminders;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Finance;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Domain.PersonalRecords.Notes;
using FinanceAssistant.Domain.PersonalRecords.Reminders;

namespace FinanceAssistant.Application.Tests.Assistant;

public sealed class ProcessAssistantMessageUseCaseTests
{
    [Fact]
    public async Task ReadToolDispatchesThroughApplicationUseCase()
    {
        var fixture = new Fixture(
            """
            {
              "name": "ReadTransactions",
              "parameters": {}
            }
            """);
        fixture.Transactions.Add(Transaction.Create(
            fixture.ProfileId,
            Money.Create(12.50m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 16),
            "Lunch",
            fixture.Category));

        var result = await fixture.Process.ExecuteAsync(new ProcessAssistantMessageRequest("show transactions"));

        Assert.True(result.Succeeded);
        Assert.Equal(AssistantToolNames.ReadTransactions, result.ToolName);
        Assert.Contains("Lunch", result.PayloadJson, StringComparison.Ordinal);
        Assert.Contains(AssistantToolNames.ReadTransactions, fixture.Model.LastRequest!.ToolSchemas.Keys);
        Assert.Equal("system prompt", fixture.Model.LastRequest.SystemPrompt);
    }

    [Fact]
    public async Task AdviceUsesStoredMonthlyData()
    {
        var fixture = new Fixture(
            """
            {
              "name": "AnalyzeSpendingPatterns",
              "parameters": {
                "year": 2026,
                "month": 6
              }
            }
            """);
        fixture.Transactions.Add(Transaction.Create(
            fixture.ProfileId,
            Money.Create(40m),
            TransactionType.Expense,
            new DateOnly(2026, 6, 5),
            "Groceries",
            fixture.Category));

        var result = await fixture.Process.ExecuteAsync(new ProcessAssistantMessageRequest("analyze June"));

        Assert.True(result.Succeeded);
        Assert.Equal(AssistantToolNames.AnalyzeSpendingPatterns, result.ToolName);
        Assert.Contains("Total expenses", result.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("40.00", result.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("Groceries", result.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteProposalCreatesConfirmationWithoutPersistingWrite()
    {
        var fixture = new Fixture(
            """
            {
              "name": "ProposeNote",
              "parameters": {
                "content": "Review subscriptions"
              }
            }
            """);

        var result = await fixture.Process.ExecuteAsync(new ProcessAssistantMessageRequest("remember this"));

        Assert.True(result.Succeeded);
        Assert.True(result.RequiresConfirmation);
        Assert.Equal(AssistantToolNames.ProposeNote, result.ToolName);
        Assert.NotNull(result.ConfirmationToken);
        Assert.NotNull(result.OperationFingerprint);
        Assert.Single(fixture.Confirmations.Records);
        Assert.Empty(fixture.Notes.Notes);
    }

    [Fact]
    public async Task HostileModelOutputReturnsControlledErrorWithoutSideEffects()
    {
        var fixture = new Fixture(
            """
            {
              "name": "ProposeNote",
              "parameters": {
                "content": "owned",
                "userId": "attacker"
              }
            }
            """);

        var result = await fixture.Process.ExecuteAsync(new ProcessAssistantMessageRequest("save note"));

        Assert.False(result.Succeeded);
        Assert.Equal("Model output must not contain identity fields.", result.Message);
        Assert.Empty(fixture.Confirmations.Records);
        Assert.Empty(fixture.Notes.Notes);
    }

    private sealed class Fixture
    {
        public Fixture(string modelOutput)
        {
            CurrentProfile = new FixedCurrentProfileProvider(ProfileId);
            Category = Category.Create(ProfileId, "Groceries", TransactionType.Expense);
            Categories.Add(Category);
            Model = new FakeAssistantModelClient(modelOutput);

            var getTransactions = new GetTransactionsUseCase(CurrentProfile, Transactions, Categories);
            var getMonthlySummary = new GetMonthlySummaryUseCase(CurrentProfile, Transactions, Categories);
            var listNotes = new ListNotesUseCase(CurrentProfile, Notes);
            var listReminders = new ListRemindersUseCase(CurrentProfile, Reminders);
            var getParsedDocument = new GetParsedDocumentUseCase(CurrentProfile, ParsedDocuments);
            var createProposal = new CreateAssistantProposalUseCase(CurrentProfile, Confirmations, Clock);

            Process = new ProcessAssistantMessageUseCase(
                new FakeAssistantPromptCatalog(),
                Model,
                new AssistantModelOutputParser(),
                getTransactions,
                getMonthlySummary,
                listNotes,
                listReminders,
                getParsedDocument,
                createProposal);
        }

        public LocalProfileId ProfileId { get; } = LocalProfileId.New();
        public FixedCurrentProfileProvider CurrentProfile { get; }
        public Category Category { get; }
        public FakeCategoryRepository Categories { get; } = new();
        public FakeTransactionRepository Transactions { get; } = new();
        public FakeNoteRepository Notes { get; } = new();
        public FakeReminderRepository Reminders { get; } = new();
        public FakeParsedDocumentRepository ParsedDocuments { get; } = new();
        public FakeAssistantConfirmationRepository Confirmations { get; } = new();
        public FixedClock Clock { get; } = new();
        public FakeAssistantModelClient Model { get; }
        public ProcessAssistantMessageUseCase Process { get; }
    }

    private sealed class FakeAssistantPromptCatalog : IAssistantPromptCatalog
    {
        public Task<string> GetSystemPromptAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("system prompt");
        }

        public Task<IReadOnlyDictionary<string, string>> GetToolSchemasAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, string> schemas = AssistantToolNames.All.ToDictionary(
                toolName => toolName,
                toolName => $$"""{ "name": "{{toolName}}" }""",
                StringComparer.Ordinal);

            return Task.FromResult(schemas);
        }
    }

    private sealed class FakeAssistantModelClient : IAssistantModelClient
    {
        private readonly string modelOutput;

        public FakeAssistantModelClient(string modelOutput)
        {
            this.modelOutput = modelOutput;
        }

        public AssistantModelRequest? LastRequest { get; private set; }

        public AssistantConfigurationDisclosure GetConfigurationDisclosure()
        {
            return new AssistantConfigurationDisclosure(
                new Uri("http://localhost:11434/v1/chat/completions"),
                "local",
                IsRemoteEndpoint: false,
                IsRemoteAllowed: false,
                RequiresRemoteDisclosure: false,
                WarningMessage: null);
        }

        public Task<AssistantModelResponse> CompleteAsync(
            AssistantModelRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(AssistantModelResponse.Available(modelOutput, GetConfigurationDisclosure()));
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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeAssistantConfirmationRepository : IAssistantConfirmationRepository
    {
        public List<AssistantConfirmationRecord> Records { get; } = [];

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
            return Task.FromResult(false);
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> categories = [];

        public void Add(Category category)
        {
            categories.Add(category);
        }

        public Task<IReadOnlyList<Category>> ListCategoriesAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(
                categories.Where(category => category.ProfileId == profileId).ToArray());
        }

        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CategorizationRule>> ListCategorizationRulesAsync(
            LocalProfileId profileId,
            TransactionType transactionType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CategorizationRule>>([]);
        }

        public Task AddCategorizationRuleAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        private readonly List<Transaction> transactions = [];

        public void Add(Transaction transaction)
        {
            transactions.Add(transaction);
        }

        public Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> ListTransactionsAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(
                transactions.Where(transaction => transaction.ProfileId == profileId).ToArray());
        }

        public Task<Transaction?> GetTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Transaction?>(
                transactions.SingleOrDefault(transaction => transaction.ProfileId == profileId && transaction.Id == transactionId));
        }

        public Task UpdateTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteTransactionAsync(
            LocalProfileId profileId,
            TransactionId transactionId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
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

        public Task<IReadOnlyList<Note>> ListNotesAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Note>>(
                Notes.Where(note => note.ProfileId == profileId).ToArray());
        }

        public Task<Note?> GetNoteAsync(
            LocalProfileId profileId,
            NoteId noteId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Note?>(Notes.SingleOrDefault(note => note.ProfileId == profileId && note.Id == noteId));
        }

        public Task DeleteNoteAsync(
            LocalProfileId profileId,
            NoteId noteId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReminderRepository : IReminderRepository
    {
        private readonly List<PaymentReminder> reminders = [];

        public Task AddReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
        {
            reminders.Add(reminder);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PaymentReminder>> ListRemindersAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PaymentReminder>>(
                reminders.Where(reminder => reminder.ProfileId == profileId).ToArray());
        }

        public Task<PaymentReminder?> GetReminderAsync(
            LocalProfileId profileId,
            PaymentReminderId reminderId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaymentReminder?>(
                reminders.SingleOrDefault(reminder => reminder.ProfileId == profileId && reminder.Id == reminderId));
        }

        public Task UpdateReminderAsync(PaymentReminder reminder, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteReminderAsync(
            LocalProfileId profileId,
            PaymentReminderId reminderId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeParsedDocumentRepository : IDocumentParsedContentRepository
    {
        public Task SaveParsedDocumentAsync(ParsedDocument parsedDocument, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<ParsedDocument?> GetParsedDocumentAsync(
            LocalProfileId profileId,
            DocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ParsedDocument?>(null);
        }
    }
}
