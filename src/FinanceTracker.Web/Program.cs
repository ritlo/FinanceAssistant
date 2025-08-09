using FinanceTracker.Web;
using FinanceTracker.Web.Components;
using FinanceTracker.Web.Services; // Add this using directive
using MudBlazor.Services;


var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


// Register MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddOutputCache();

// Use ApiServiceBaseUrl from configuration
var apiBaseUrl = builder.Configuration["ApiServiceBaseUrl"] ?? "http://localhost:5409/";

builder.Services.AddHttpClient<IAgentApiClient, AgentApiClient>(client =>
{
    client.BaseAddress = new(apiBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<TransactionApiClient>(client =>
{
    client.BaseAddress = new(apiBaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
