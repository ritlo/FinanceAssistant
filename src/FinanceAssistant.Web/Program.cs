using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Application.Assistant.Confirmations.CancelAssistantProposal;
using FinanceAssistant.Application.Assistant.Confirmations.ConfirmAssistantProposal;
using FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;
using FinanceAssistant.Application.Assistant.ProcessMessage;
using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Application.Documents.CreateDocumentRecord;
using FinanceAssistant.Application.Documents.GetParsedDocument;
using FinanceAssistant.Application.Documents.ListDocuments;
using FinanceAssistant.Application.Documents.ParseDocument;
using FinanceAssistant.Application.Documents.UpdateDocumentStatus;
using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ListCategories;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Notes;
using FinanceAssistant.Application.PersonalRecords.Notes.CreateNote;
using FinanceAssistant.Application.PersonalRecords.Notes.DeleteNote;
using FinanceAssistant.Application.PersonalRecords.Notes.ListNotes;
using FinanceAssistant.Application.PersonalRecords.Reminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;
using FinanceAssistant.Application.PersonalRecords.Reminders.DeleteReminder;
using FinanceAssistant.Application.PersonalRecords.Reminders.ListReminders;
using FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderPaid;
using FinanceAssistant.Application.PersonalRecords.Reminders.MarkReminderUnpaid;
using FinanceAssistant.Infrastructure.Assistant;
using FinanceAssistant.Infrastructure.Common;
using FinanceAssistant.Infrastructure.Documents;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Finance.Transactions;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Infrastructure.PersonalRecords.Notes;
using FinanceAssistant.Infrastructure.PersonalRecords.Reminders;
using FinanceAssistant.Web.Components;
using FinanceAssistant.Web.Finance.Transactions;

var builder = WebApplication.CreateBuilder(args);

var dataOptions = new FinanceAssistantDataOptions
{
    DatabasePath = builder.Configuration["FinanceAssistant:DatabasePath"]
        ?? FinanceAssistantDataOptions.DefaultDatabasePath(),
    DocumentTemporaryDirectoryPath = builder.Configuration["FinanceAssistant:DocumentTemporaryDirectoryPath"]
        ?? FinanceAssistantDataOptions.DefaultDocumentTemporaryDirectoryPath(),
    Currency = builder.Configuration["FinanceAssistant:Currency"] ?? string.Empty,
};
builder.Services.AddSingleton(dataOptions);
builder.Services.AddSingleton(new AssistantModelOptions
{
    Endpoint = builder.Configuration["FinanceAssistant:Assistant:Endpoint"]
        ?? AssistantModelOptions.DefaultEndpoint,
    Model = builder.Configuration["FinanceAssistant:Assistant:Model"]
        ?? AssistantModelOptions.DefaultModel,
    ApiKey = builder.Configuration["FinanceAssistant:Assistant:ApiKey"],
    AllowRemote = bool.TryParse(builder.Configuration["FinanceAssistant:Assistant:AllowRemote"], out var allowRemote)
        && allowRemote,
});
builder.Services.AddSingleton<LiteDbSchemaInitializer>();
builder.Services.AddScoped<ICurrentProfileProvider, LiteDbCurrentProfileProvider>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICategoryRepository, LiteDbCategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, LiteDbTransactionRepository>();
builder.Services.AddScoped<INoteRepository, LiteDbNoteRepository>();
builder.Services.AddScoped<IReminderRepository, LiteDbPaymentReminderRepository>();
builder.Services.AddScoped<IDocumentMetadataRepository, LiteDbDocumentMetadataRepository>();
builder.Services.AddScoped<IDocumentParsedContentRepository, LiteDbParsedDocumentRepository>();
builder.Services.AddScoped<IDocumentTemporaryStorage, FileSystemDocumentTemporaryStorage>();
builder.Services.AddScoped<IDocumentParser, LocalDocumentParser>();
builder.Services.AddScoped<IAssistantConfirmationRepository, LiteDbAssistantConfirmationRepository>();
builder.Services.AddSingleton<IAssistantPromptCatalog, FileAssistantPromptCatalog>();
builder.Services.AddSingleton<AssistantModelOutputParser>();
builder.Services.AddSingleton<InProcessTransactionChangeNotifier>();
builder.Services.AddSingleton<ITransactionChangeNotifier>(
    services => services.GetRequiredService<InProcessTransactionChangeNotifier>());
builder.Services.AddHttpClient<IAssistantModelClient, OpenAiCompatibleAssistantModelClient>();
builder.Services.AddScoped<TransactionDashboardState>();
builder.Services.AddScoped<ListCategoriesUseCase>();
builder.Services.AddScoped<LogTransactionUseCase>();
builder.Services.AddScoped<GetTransactionsUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();
builder.Services.AddScoped<GetMonthlySummaryUseCase>();
builder.Services.AddScoped<CreateNoteUseCase>();
builder.Services.AddScoped<ListNotesUseCase>();
builder.Services.AddScoped<DeleteNoteUseCase>();
builder.Services.AddScoped<CreateReminderUseCase>();
builder.Services.AddScoped<ListRemindersUseCase>();
builder.Services.AddScoped<DeleteReminderUseCase>();
builder.Services.AddScoped<MarkReminderPaidUseCase>();
builder.Services.AddScoped<MarkReminderUnpaidUseCase>();
builder.Services.AddScoped<CreateDocumentRecordUseCase>();
builder.Services.AddScoped<ParseDocumentUseCase>();
builder.Services.AddScoped<ListDocumentsUseCase>();
builder.Services.AddScoped<GetParsedDocumentUseCase>();
builder.Services.AddScoped<UpdateDocumentStatusUseCase>();
builder.Services.AddScoped<CreateAssistantProposalUseCase>();
builder.Services.AddScoped<ConfirmAssistantProposalUseCase>();
builder.Services.AddScoped<CancelAssistantProposalUseCase>();
builder.Services.AddScoped<ProcessAssistantMessageUseCase>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.Services.GetRequiredService<LiteDbSchemaInitializer>().Initialize();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
