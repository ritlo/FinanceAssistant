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

// Register AgentApiClient as IAgentApiClient for DI abstraction
builder.Services.AddHttpClient<IAgentApiClient, AgentApiClient>(client =>
{
    client.BaseAddress = new("http://apiservice"); // apiService is the name of our backend project in Aspire
    client.Timeout = TimeSpan.FromMinutes(5); // Set a longer timeout for streaming
});

builder.Services.AddHttpClient<TransactionApiClient>(client =>
{
    client.BaseAddress = new("http://apiservice");
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
