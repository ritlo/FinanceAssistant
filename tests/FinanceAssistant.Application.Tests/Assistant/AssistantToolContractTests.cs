using System.Text.Json;
using FinanceAssistant.Application.Assistant;

namespace FinanceAssistant.Application.Tests.Assistant;

public sealed class AssistantToolContractTests
{
    [Fact]
    public void EveryInitialToolSchemaFileIsPresent()
    {
        var schemaDirectory = Path.Combine(RepositoryRoot(), "src", "FinanceAssistant.Application", "Assistant", "ToolSchemas", "v1");

        foreach (var toolName in AssistantToolNames.All)
        {
            var path = Path.Combine(schemaDirectory, $"{toolName}.json");
            Assert.True(File.Exists(path), $"Missing schema file for {toolName}.");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(toolName, document.RootElement.GetProperty("name").GetString());
        }
    }

    [Fact]
    public void PromptVersionFileIsPresent()
    {
        var path = Path.Combine(
            RepositoryRoot(),
            "src",
            "FinanceAssistant.Application",
            "Assistant",
            "Prompts",
            "v1",
            "system.md");

        var prompt = File.ReadAllText(path);

        Assert.Contains("local-first assistant", prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Writes must be returned only as typed proposals", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void MalformedModelOutputReturnsControlledError()
    {
        var result = new AssistantModelOutputParser().Parse("{not-json");

        Assert.False(result.Succeeded);
        Assert.Equal("Model output was not valid JSON.", result.ErrorMessage);
    }

    [Fact]
    public void HostileOutputContainingUserIdIsRejected()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "ReadTransactions",
              "parameters": {
                "userId": "attacker"
              }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Equal("Model output must not contain identity fields.", result.ErrorMessage);
    }

    [Fact]
    public void UnknownToolIsRejected()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "LogTransaction",
              "parameters": {
                "amount": 10,
                "description": "coffee",
                "date": "2026-06-16"
              }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Equal("Model requested an unsupported tool.", result.ErrorMessage);
    }

    [Fact]
    public void ReadToolsCanBeRepresented()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "ReadTransactions",
              "parameters": {}
            }
            """);

        var toolCall = Assert.IsType<AssistantReadToolCall>(result.ToolCall);
        Assert.True(result.Succeeded);
        Assert.Equal(AssistantToolNames.ReadTransactions, toolCall.Name);
        Assert.Equal(AssistantToolCallKind.Read, toolCall.Kind);
    }

    [Fact]
    public void AdviceRequestsRequireExplicitPeriod()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "AnalyzeSpendingPatterns",
              "parameters": {
                "month": 6
              }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Equal("Advice requests must include year and month.", result.ErrorMessage);
    }

    [Fact]
    public void AdviceRequestProducesStructuredContractWithoutSideEffects()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "AnalyzeSpendingPatterns",
              "parameters": {
                "year": 2026,
                "month": 6
              }
            }
            """);

        var toolCall = Assert.IsType<AssistantAdviceToolCall>(result.ToolCall);
        var noData = AnalyzeSpendingPatternsResult.NoData(2026, 6, "No transactions were found for this month.");

        Assert.Equal(AssistantToolCallKind.Advice, toolCall.Kind);
        Assert.Equal(2026, toolCall.Request.Year);
        Assert.Equal(6, toolCall.Request.Month);
        Assert.False(noData.HasSufficientData);
        Assert.Empty(noData.ObservedFacts);
        Assert.Empty(noData.Recommendations);
        Assert.Empty(noData.BudgetSuggestions);
        Assert.Equal("No transactions were found for this month.", noData.NoDataReason);
    }

    [Fact]
    public void AdviceOutputCannotRequestWritesExceptThroughProposalRecords()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "AnalyzeSpendingPatterns",
              "parameters": {
                "year": 2026,
                "month": 6,
                "proposedTransaction": {
                  "amount": 12
                }
              }
            }
            """);

        Assert.False(result.Succeeded);
        Assert.Equal("Advice request contains unsupported fields.", result.ErrorMessage);
    }

    [Fact]
    public void WriteToolsProduceTypedProposalRecordsWithoutPersistence()
    {
        var result = new AssistantModelOutputParser().Parse(
            """
            {
              "name": "ProposeTransaction",
              "parameters": {
                "amount": 12.50,
                "description": "Lunch",
                "date": "2026-06-16",
                "transactionType": "Expense",
                "categoryName": "Food and Drinks"
              }
            }
            """);

        var toolCall = Assert.IsType<AssistantWriteProposalToolCall>(result.ToolCall);
        var proposal = Assert.IsType<ProposeTransactionProposal>(toolCall.Proposal);
        Assert.Equal(AssistantToolNames.ProposeTransaction, toolCall.Name);
        Assert.Equal(AssistantToolCallKind.WriteProposal, toolCall.Kind);
        Assert.Equal(12.50m, proposal.Amount);
        Assert.Equal("Lunch", proposal.Description);
        Assert.Equal(new DateOnly(2026, 6, 16), proposal.Date);
        Assert.Equal("Expense", proposal.TransactionType);
        Assert.Equal("Food and Drinks", proposal.CategoryName);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
