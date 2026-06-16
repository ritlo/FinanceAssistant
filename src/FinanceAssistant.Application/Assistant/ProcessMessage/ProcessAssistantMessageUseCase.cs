using System.Text.Json;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;
using FinanceAssistant.Application.Documents.GetParsedDocument;
using FinanceAssistant.Application.Finance.Summaries.GetMonthlySummary;
using FinanceAssistant.Application.Finance.Transactions;
using FinanceAssistant.Application.Finance.Transactions.GetTransactions;
using FinanceAssistant.Application.PersonalRecords.Notes.ListNotes;
using FinanceAssistant.Application.PersonalRecords.Reminders.ListReminders;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Assistant.ProcessMessage;

public sealed class ProcessAssistantMessageUseCase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly IAssistantPromptCatalog promptCatalog;
    private readonly IAssistantModelClient modelClient;
    private readonly AssistantModelOutputParser parser;
    private readonly GetTransactionsUseCase getTransactions;
    private readonly GetMonthlySummaryUseCase getMonthlySummary;
    private readonly ListNotesUseCase listNotes;
    private readonly ListRemindersUseCase listReminders;
    private readonly GetParsedDocumentUseCase getParsedDocument;
    private readonly CreateAssistantProposalUseCase createProposal;

    public ProcessAssistantMessageUseCase(
        IAssistantPromptCatalog promptCatalog,
        IAssistantModelClient modelClient,
        AssistantModelOutputParser parser,
        GetTransactionsUseCase getTransactions,
        GetMonthlySummaryUseCase getMonthlySummary,
        ListNotesUseCase listNotes,
        ListRemindersUseCase listReminders,
        GetParsedDocumentUseCase getParsedDocument,
        CreateAssistantProposalUseCase createProposal)
    {
        this.promptCatalog = promptCatalog;
        this.modelClient = modelClient;
        this.parser = parser;
        this.getTransactions = getTransactions;
        this.getMonthlySummary = getMonthlySummary;
        this.listNotes = listNotes;
        this.listReminders = listReminders;
        this.getParsedDocument = getParsedDocument;
        this.createProposal = createProposal;
    }

    public async Task<ProcessAssistantMessageResult> ExecuteAsync(
        ProcessAssistantMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new DomainValidationException("Assistant message is required.");
        }

        var modelRequest = new AssistantModelRequest(
            await promptCatalog.GetSystemPromptAsync(cancellationToken),
            request.Message,
            await promptCatalog.GetToolSchemasAsync(cancellationToken));

        var modelResponse = await modelClient.CompleteAsync(modelRequest, cancellationToken);
        if (!modelResponse.IsAvailable || string.IsNullOrWhiteSpace(modelResponse.Content))
        {
            return ProcessAssistantMessageResult.Error(
                modelResponse.ErrorMessage ?? "Assistant endpoint is unavailable.");
        }

        var parseResult = parser.Parse(modelResponse.Content);
        if (!parseResult.Succeeded || parseResult.ToolCall is null)
        {
            return ProcessAssistantMessageResult.Error(parseResult.ErrorMessage ?? "Assistant output could not be parsed.");
        }

        return parseResult.ToolCall switch
        {
            AssistantReadToolCall read => await ExecuteReadAsync(read, cancellationToken),
            AssistantAdviceToolCall advice => await ExecuteAdviceAsync(advice, cancellationToken),
            AssistantWriteProposalToolCall proposal => await ExecuteWriteProposalAsync(proposal, cancellationToken),
            _ => ProcessAssistantMessageResult.Error("Assistant requested an unsupported action."),
        };
    }

    private async Task<ProcessAssistantMessageResult> ExecuteReadAsync(
        AssistantReadToolCall toolCall,
        CancellationToken cancellationToken)
    {
        object payload = toolCall.Name switch
        {
            AssistantToolNames.ReadTransactions => await getTransactions.ExecuteAsync(cancellationToken),
            AssistantToolNames.GetMonthlySummary => await GetMonthlySummaryAsync(toolCall.Parameters, cancellationToken),
            AssistantToolNames.GetNotes => await listNotes.ExecuteAsync(cancellationToken),
            AssistantToolNames.GetPaymentReminders => await listReminders.ExecuteAsync(cancellationToken),
            AssistantToolNames.ReadParsedDocument => await ReadParsedDocumentAsync(toolCall.Parameters, cancellationToken),
            _ => throw new DomainValidationException("Assistant requested an unsupported read tool."),
        };

        return ProcessAssistantMessageResult.Success(
            $"{toolCall.Name} completed.",
            toolCall.Name,
            toolCall.Kind,
            Serialize(payload));
    }

    private async Task<ProcessAssistantMessageResult> ExecuteAdviceAsync(
        AssistantAdviceToolCall toolCall,
        CancellationToken cancellationToken)
    {
        var result = await AnalyzeSpendingAsync(toolCall.Request, cancellationToken);

        return ProcessAssistantMessageResult.Success(
            "Spending analysis completed.",
            toolCall.Name,
            toolCall.Kind,
            Serialize(result));
    }

    private async Task<ProcessAssistantMessageResult> ExecuteWriteProposalAsync(
        AssistantWriteProposalToolCall toolCall,
        CancellationToken cancellationToken)
    {
        var confirmation = await createProposal.ExecuteAsync(
            new CreateAssistantProposalRequest(toolCall.Name, toolCall.Proposal),
            cancellationToken);

        return ProcessAssistantMessageResult.Success(
            "Assistant proposal requires confirmation.",
            toolCall.Name,
            toolCall.Kind,
            Serialize(confirmation),
            confirmation.Token,
            confirmation.OperationFingerprint);
    }

    private async Task<GetMonthlySummaryResult> GetMonthlySummaryAsync(
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        return await getMonthlySummary.ExecuteAsync(
            new GetMonthlySummaryRequest(
                GetRequiredInt(parameters, "year"),
                GetRequiredInt(parameters, "month")),
            cancellationToken);
    }

    private async Task<object> ReadParsedDocumentAsync(
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        var documentId = GetRequiredGuid(parameters, "documentId");
        var result = await getParsedDocument.ExecuteAsync(
            new GetParsedDocumentRequest(documentId),
            cancellationToken);

        return result is null
            ? new { documentId, found = false }
            : result;
    }

    private async Task<AnalyzeSpendingPatternsResult> AnalyzeSpendingAsync(
        AnalyzeSpendingPatternsRequest request,
        CancellationToken cancellationToken)
    {
        var summary = await getMonthlySummary.ExecuteAsync(
            new GetMonthlySummaryRequest(request.Year, request.Month),
            cancellationToken);

        if (summary.ExpenseTotal <= 0m)
        {
            return AnalyzeSpendingPatternsResult.NoData(
                request.Year,
                request.Month,
                "No expense transactions were found for this month.");
        }

        var transactions = await getTransactions.ExecuteAsync(cancellationToken);
        var monthlyExpenses = transactions
            .Where(transaction =>
                transaction.Type == TransactionType.Expense
                && transaction.Date.Year == request.Year
                && transaction.Date.Month == request.Month)
            .ToArray();
        var topCategory = summary.Categories.FirstOrDefault();

        var facts = new List<string>
        {
            $"Total expenses for {request.Year:D4}-{request.Month:D2} were {summary.ExpenseTotal:0.00}.",
            $"{monthlyExpenses.Length} expense transaction(s) were found for the month.",
        };
        if (topCategory is not null)
        {
            facts.Add($"The largest category was {topCategory.CategoryName} at {topCategory.Total:0.00}.");
        }

        var recommendations = new List<string>();
        if (topCategory is not null && topCategory.Total >= summary.ExpenseTotal / 2m)
        {
            recommendations.Add($"Review {topCategory.CategoryName}; it accounts for at least half of monthly expenses.");
        }
        else
        {
            recommendations.Add("Review category totals for recurring expenses before changing budgets.");
        }

        return new AnalyzeSpendingPatternsResult(
            request.Year,
            request.Month,
            HasSufficientData: true,
            facts,
            recommendations,
            ["Set category targets using the monthly totals returned with this analysis."],
            NoDataReason: null);
    }

    private static int GetRequiredInt(JsonElement parameters, string propertyName)
    {
        if (!parameters.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out var value))
        {
            throw new DomainValidationException($"Assistant tool parameter '{propertyName}' is required.");
        }

        return value;
    }

    private static Guid GetRequiredGuid(JsonElement parameters, string propertyName)
    {
        if (!parameters.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || !Guid.TryParse(property.GetString(), out var value))
        {
            throw new DomainValidationException($"Assistant tool parameter '{propertyName}' is required.");
        }

        return value;
    }

    private static string Serialize(object payload)
    {
        return JsonSerializer.Serialize(payload, SerializerOptions);
    }
}
