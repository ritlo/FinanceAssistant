using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Application.Assistant.Confirmations.CreateAssistantProposal;
using FinanceAssistant.Application.Common;
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

    private static readonly Regex AmountPattern = new(
        @"(?ix)(?:\$|usd\s*)\s*(?<amount>\d+(?:\.\d{1,2})?)|(?<amount>\d+(?:\.\d{1,2})?)\s*(?:dollars|usd)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAssistantPromptCatalog promptCatalog;
    private readonly IAssistantModelClient modelClient;
    private readonly AssistantModelOutputParser parser;
    private readonly IClock clock;
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
        IClock clock,
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
        this.clock = clock;
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

        var userMessage = request.Message.Trim();
        var localResult = await TryHandleLocalFinanceIntentAsync(userMessage, request, cancellationToken);
        if (localResult is not null)
        {
            return localResult;
        }

        var modelRequest = new AssistantModelRequest(
            await promptCatalog.GetSystemPromptAsync(cancellationToken),
            userMessage,
            await promptCatalog.GetToolSchemasAsync(cancellationToken),
            BuildRuntimeContext(request));

        var modelResponse = await modelClient.CompleteAsync(modelRequest, cancellationToken);
        if (!modelResponse.IsAvailable || string.IsNullOrWhiteSpace(modelResponse.Content))
        {
            return ProcessAssistantMessageResult.Error(
                modelResponse.ErrorMessage ?? "Assistant endpoint is unavailable.");
        }

        var modelContent = modelResponse.Content.Trim();
        var parseResult = parser.Parse(modelContent);
        if (!parseResult.Succeeded || parseResult.ToolCall is null)
        {
            if (IsPlainChatText(modelContent))
            {
                return ProcessAssistantMessageResult.Chat(modelContent);
            }

            return ProcessAssistantMessageResult.Error(FriendlyParseError(parseResult.ErrorMessage));
        }

        return parseResult.ToolCall switch
        {
            AssistantReadToolCall read => await ExecuteReadAsync(read, cancellationToken),
            AssistantAdviceToolCall advice => await ExecuteAdviceAsync(advice, cancellationToken),
            AssistantWriteProposalToolCall proposal => await ExecuteWriteProposalAsync(proposal, cancellationToken),
            _ => ProcessAssistantMessageResult.Error("Assistant requested an unsupported action."),
        };
    }

    private async Task<ProcessAssistantMessageResult?> TryHandleLocalFinanceIntentAsync(
        string userMessage,
        ProcessAssistantMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (TryCreateTransactionProposal(userMessage, out var proposal))
        {
            return await CreateTransactionProposalAsync(proposal, cancellationToken);
        }

        if (IsMonthlySpendQuestion(userMessage))
        {
            var period = GetRequestPeriod(request);
            var summary = await getMonthlySummary.ExecuteAsync(
                new GetMonthlySummaryRequest(period.Year, period.Month),
                cancellationToken);

            return ProcessAssistantMessageResult.Success(
                FormatMonthlySummary(summary),
                AssistantToolNames.GetMonthlySummary,
                AssistantToolCallKind.Read);
        }

        return null;
    }

    private async Task<ProcessAssistantMessageResult> ExecuteReadAsync(
        AssistantReadToolCall toolCall,
        CancellationToken cancellationToken)
    {
        return toolCall.Name switch
        {
            AssistantToolNames.ReadTransactions => ProcessAssistantMessageResult.Success(
                FormatTransactions(await getTransactions.ExecuteAsync(cancellationToken)),
                toolCall.Name,
                toolCall.Kind),
            AssistantToolNames.GetMonthlySummary => ProcessAssistantMessageResult.Success(
                FormatMonthlySummary(await GetMonthlySummaryAsync(toolCall.Parameters, cancellationToken)),
                toolCall.Name,
                toolCall.Kind),
            AssistantToolNames.GetNotes => ProcessAssistantMessageResult.Success(
                $"Found {(await listNotes.ExecuteAsync(cancellationToken)).Count} note(s).",
                toolCall.Name,
                toolCall.Kind),
            AssistantToolNames.GetPaymentReminders => ProcessAssistantMessageResult.Success(
                $"Found {(await listReminders.ExecuteAsync(cancellationToken)).Count} payment reminder(s).",
                toolCall.Name,
                toolCall.Kind),
            AssistantToolNames.ReadParsedDocument => ProcessAssistantMessageResult.Success(
                FormatParsedDocument(await ReadParsedDocumentAsync(toolCall.Parameters, cancellationToken)),
                toolCall.Name,
                toolCall.Kind),
            _ => throw new DomainValidationException("Assistant requested an unsupported read tool."),
        };
    }

    private async Task<ProcessAssistantMessageResult> ExecuteAdviceAsync(
        AssistantAdviceToolCall toolCall,
        CancellationToken cancellationToken)
    {
        var result = await AnalyzeSpendingAsync(toolCall.Request, cancellationToken);

        return ProcessAssistantMessageResult.Success(
            FormatSpendingAnalysis(result),
            toolCall.Name,
            toolCall.Kind);
    }

    private async Task<ProcessAssistantMessageResult> ExecuteWriteProposalAsync(
        AssistantWriteProposalToolCall toolCall,
        CancellationToken cancellationToken)
    {
        if (toolCall.Proposal is ProposeTransactionProposal transactionProposal)
        {
            return await CreateTransactionProposalAsync(transactionProposal, cancellationToken);
        }

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

    private async Task<ProcessAssistantMessageResult> CreateTransactionProposalAsync(
        ProposeTransactionProposal proposal,
        CancellationToken cancellationToken)
    {
        var confirmation = await createProposal.ExecuteAsync(
            new CreateAssistantProposalRequest(AssistantToolNames.ProposeTransaction, proposal),
            cancellationToken);

        return ProcessAssistantMessageResult.Success(
            $"I prepared an {proposal.TransactionType?.ToLowerInvariant() ?? "transaction"} preview for {proposal.Amount:0.00}: {proposal.Description}. Review it to confirm before anything is saved.",
            AssistantToolNames.ProposeTransaction,
            AssistantToolCallKind.WriteProposal,
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

    private bool TryCreateTransactionProposal(string userMessage, out ProposeTransactionProposal proposal)
    {
        proposal = default!;
        if (!LooksLikeTransactionCommand(userMessage))
        {
            return false;
        }

        var match = AmountPattern.Match(userMessage);
        if (!match.Success || !decimal.TryParse(match.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        var description = ExtractTransactionDescription(userMessage, match.Index + match.Length);
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var date = ResolveTransactionDate(userMessage);
        var transactionType = LooksLikeIncome(userMessage) ? "Income" : "Expense";
        proposal = new ProposeTransactionProposal(amount, description, date, transactionType, CategoryName: null);
        return true;
    }

    private static bool LooksLikeTransactionCommand(string userMessage)
    {
        return ContainsAny(userMessage, "spent", "paid", "bought", "purchased", "add transaction", "transaction:", "expense");
    }

    private static bool LooksLikeIncome(string userMessage)
    {
        return ContainsAny(userMessage, "income", "salary", "earned", "received", "deposit");
    }

    private static bool IsMonthlySpendQuestion(string userMessage)
    {
        return ContainsAny(userMessage, "spent", "spend", "expenses", "expense")
            && ContainsAny(userMessage, "month", "monthly", "this month");
    }

    private DateOnly ResolveTransactionDate(string userMessage)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.LocalDateTime);
        if (ContainsAny(userMessage, "yesterday"))
        {
            return today.AddDays(-1);
        }

        return today;
    }

    private static string ExtractTransactionDescription(string userMessage, int startIndex)
    {
        var text = userMessage[startIndex..].Trim();
        text = Regex.Replace(text, @"^(?:at|in|for|to|on|from)\s+", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"\b(?:this\s+morning|this\s+afternoon|this\s+evening|today|yesterday)\b", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"\s+", " ").Trim(' ', '.', '!', '?', ',');
        return text;
    }

    private string BuildRuntimeContext(ProcessAssistantMessageRequest request)
    {
        var period = GetRequestPeriod(request);
        var currentDate = DateOnly.FromDateTime(clock.UtcNow.LocalDateTime);
        return $"Current local date: {currentDate:O}. Active summary period: {period.Year:D4}-{period.Month:D2}. Interpret 'this month' as the active summary period.";
    }

    private (int Year, int Month) GetRequestPeriod(ProcessAssistantMessageRequest request)
    {
        if (request.ContextYear is not null && request.ContextMonth is >= 1 and <= 12)
        {
            return (request.ContextYear.Value, request.ContextMonth.Value);
        }

        var currentDate = DateOnly.FromDateTime(clock.UtcNow.LocalDateTime);
        return (currentDate.Year, currentDate.Month);
    }

    private static string FormatTransactions(IReadOnlyList<TransactionResult> transactions)
    {
        if (transactions.Count == 0)
        {
            return "No transactions are recorded yet.";
        }

        var latest = transactions.OrderByDescending(transaction => transaction.Date).First();
        return $"Found {transactions.Count} transaction(s). Latest: {latest.Date:O} {latest.Description} {latest.Amount:0.00}.";
    }

    private static string FormatMonthlySummary(GetMonthlySummaryResult summary)
    {
        var monthName = new DateOnly(summary.Year, summary.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        if (summary.ExpenseTotal <= 0m)
        {
            return $"You spent 0.00 in {monthName}. No expense transactions were found.";
        }

        var topCategory = summary.Categories.FirstOrDefault();
        return topCategory is null
            ? $"You spent {summary.ExpenseTotal:0.00} in {monthName}."
            : $"You spent {summary.ExpenseTotal:0.00} in {monthName}. Largest category: {topCategory.CategoryName} at {topCategory.Total:0.00}.";
    }

    private static string FormatSpendingAnalysis(AnalyzeSpendingPatternsResult result)
    {
        if (!result.HasSufficientData)
        {
            return result.NoDataReason ?? "No spending data was available for analysis.";
        }

        return string.Join(" ", result.ObservedFacts.Concat(result.Recommendations).Concat(result.BudgetSuggestions));
    }

    private static string FormatParsedDocument(object result)
    {
        var payload = Serialize(result);
        return payload.Contains("\"found\": false", StringComparison.Ordinal)
            ? "Parsed document was not found."
            : "Parsed document was found.";
    }

    private static bool IsPlainChatText(string modelOutput)
    {
        var trimmed = modelOutput.TrimStart();
        return !trimmed.StartsWith('{') && !trimmed.StartsWith('[');
    }

    private static string FriendlyParseError(string? parseError)
    {
        return string.Equals(parseError, "Model output was not valid JSON.", StringComparison.Ordinal)
            ? "I could not understand the assistant response. Please try again or rephrase your request."
            : parseError ?? "Assistant output could not be parsed.";
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
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
