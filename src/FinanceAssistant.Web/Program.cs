using FinanceAssistant.Application.Finance.Categories;
using FinanceAssistant.Application.Finance.Categories.ListCategories;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.DeleteTransaction;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Infrastructure.Finance.Categories;
using FinanceAssistant.Infrastructure.Finance.Transactions;
using FinanceAssistant.Infrastructure.Identity;
using FinanceAssistant.Infrastructure.Persistence;
using FinanceAssistant.Web.Components;

var builder = WebApplication.CreateBuilder(args);

var dataOptions = new FinanceAssistantDataOptions
{
    DatabasePath = builder.Configuration["FinanceAssistant:DatabasePath"]
        ?? FinanceAssistantDataOptions.DefaultDatabasePath(),
    Currency = builder.Configuration["FinanceAssistant:Currency"] ?? string.Empty,
};
builder.Services.AddSingleton(dataOptions);
builder.Services.AddSingleton<LiteDbSchemaInitializer>();
builder.Services.AddScoped<ICurrentProfileProvider, LiteDbCurrentProfileProvider>();
builder.Services.AddScoped<ICategoryRepository, LiteDbCategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, LiteDbTransactionRepository>();
builder.Services.AddSingleton<ITransactionChangeNotifier, InProcessTransactionChangeNotifier>();
builder.Services.AddScoped<ListCategoriesUseCase>();
builder.Services.AddScoped<LogTransactionUseCase>();
builder.Services.AddScoped<GetTransactionsUseCase>();
builder.Services.AddScoped<UpdateTransactionUseCase>();
builder.Services.AddScoped<DeleteTransactionUseCase>();

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
