
using FinanceTracker.DatabaseReader.Services;


var builder = WebApplication.CreateBuilder(args);
// Register LiteDbTransactionReader with the path to the main app's database
var dbPath = "../../src/FinanceTracker.ApiService/Data/FinanceTracker.db";
builder.Services.AddSingleton(new LiteDbTransactionReader(dbPath));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();

// Map Razor components (replace 'App' with your root component if needed)
app.MapRazorComponents<FinanceTracker.DatabaseReader.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
