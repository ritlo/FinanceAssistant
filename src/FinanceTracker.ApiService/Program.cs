using FinanceTracker.ApiService.Data;
using FinanceTracker.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for streaming
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MinResponseDataRate = null; // Disable minimum data rate for streaming
    serverOptions.Limits.MinRequestBodyDataRate = null; // Disable minimum data rate for streaming
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Increase keep-alive timeout
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2); // Increase request headers timeout
});

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Configure LiteDB
var dbPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "Data", "FinanceTracker.db");
builder.Services.AddSingleton<ILiteDbContext>(new LiteDbContext(dbPath));
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<AgentService>(); // Register AgentService

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add configuration for OpenAI/Local LLM
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// Seed initial categories
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var transactionService = services.GetRequiredService<TransactionService>();
    transactionService.SeedCategories();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
