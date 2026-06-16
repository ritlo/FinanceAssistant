using System.Globalization;
using System.Text.Json;

namespace FinanceAssistant.Application.Assistant;

public sealed class AssistantModelOutputParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public AssistantModelParseResult Parse(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            return AssistantModelParseResult.Error("Model output was empty.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(modelOutput);
        }
        catch (JsonException)
        {
            return AssistantModelParseResult.Error("Model output was not valid JSON.");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return AssistantModelParseResult.Error("Model output must be a JSON object.");
            }

            if (ContainsIdentityField(root))
            {
                return AssistantModelParseResult.Error("Model output must not contain identity fields.");
            }

            if (HasUnsupportedTopLevelFields(root))
            {
                return AssistantModelParseResult.Error("Model output contains unsupported fields.");
            }

            if (!TryGetString(root, "name", out var toolName) || string.IsNullOrWhiteSpace(toolName))
            {
                return AssistantModelParseResult.Error("Model output must include a tool name.");
            }

            if (!AssistantToolNames.All.Contains(toolName))
            {
                return AssistantModelParseResult.Error("Model requested an unsupported tool.");
            }

            var parameters = GetParameters(root);
            if (parameters.ValueKind != JsonValueKind.Object)
            {
                return AssistantModelParseResult.Error("Tool parameters must be a JSON object.");
            }

            return toolName switch
            {
                AssistantToolNames.AnalyzeSpendingPatterns => ParseAdvice(parameters),
                AssistantToolNames.ProposeTransaction => ParseTransactionProposal(parameters),
                AssistantToolNames.ProposeNote => ParseNoteProposal(parameters),
                AssistantToolNames.ProposePaymentReminder => ParsePaymentReminderProposal(parameters),
                _ => AssistantModelParseResult.Success(new AssistantReadToolCall(toolName, parameters.Clone())),
            };
        }
    }

    private static AssistantModelParseResult ParseAdvice(JsonElement parameters)
    {
        if (HasUnsupportedFields(parameters, ["year", "month"]))
        {
            return AssistantModelParseResult.Error("Advice request contains unsupported fields.");
        }

        if (!TryGetInt(parameters, "year", out var year) || !TryGetInt(parameters, "month", out var month))
        {
            return AssistantModelParseResult.Error("Advice requests must include year and month.");
        }

        if (month is < 1 or > 12)
        {
            return AssistantModelParseResult.Error("Advice request month must be between 1 and 12.");
        }

        return AssistantModelParseResult.Success(
            new AssistantAdviceToolCall(new AnalyzeSpendingPatternsRequest(year, month)));
    }

    private static AssistantModelParseResult ParseTransactionProposal(JsonElement parameters)
    {
        if (HasUnsupportedFields(parameters, ["amount", "description", "date", "transactionType", "categoryName"]))
        {
            return AssistantModelParseResult.Error("Transaction proposal contains unsupported fields.");
        }

        if (!TryGetDecimal(parameters, "amount", out var amount)
            || !TryGetString(parameters, "description", out var description)
            || !TryGetDate(parameters, "date", out var date))
        {
            return AssistantModelParseResult.Error("Transaction proposal is missing required fields.");
        }

        TryGetString(parameters, "transactionType", out var transactionType);
        TryGetString(parameters, "categoryName", out var categoryName);

        return AssistantModelParseResult.Success(
            new AssistantWriteProposalToolCall(
                AssistantToolNames.ProposeTransaction,
                new ProposeTransactionProposal(amount, description, date, transactionType, categoryName)));
    }

    private static AssistantModelParseResult ParseNoteProposal(JsonElement parameters)
    {
        if (HasUnsupportedFields(parameters, ["content"]))
        {
            return AssistantModelParseResult.Error("Note proposal contains unsupported fields.");
        }

        if (!TryGetString(parameters, "content", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return AssistantModelParseResult.Error("Note proposal is missing content.");
        }

        return AssistantModelParseResult.Success(
            new AssistantWriteProposalToolCall(AssistantToolNames.ProposeNote, new ProposeNoteProposal(content)));
    }

    private static AssistantModelParseResult ParsePaymentReminderProposal(JsonElement parameters)
    {
        if (HasUnsupportedFields(parameters, ["content", "dueDate"]))
        {
            return AssistantModelParseResult.Error("Payment reminder proposal contains unsupported fields.");
        }

        if (!TryGetString(parameters, "content", out var content)
            || !TryGetDate(parameters, "dueDate", out var dueDate))
        {
            return AssistantModelParseResult.Error("Payment reminder proposal is missing required fields.");
        }

        return AssistantModelParseResult.Success(
            new AssistantWriteProposalToolCall(
                AssistantToolNames.ProposePaymentReminder,
                new ProposePaymentReminderProposal(content, dueDate)));
    }

    private static JsonElement GetParameters(JsonElement root)
    {
        return root.TryGetProperty("parameters", out var parameters)
            ? parameters
            : JsonSerializer.SerializeToElement(new { }, SerializerOptions);
    }

    private static bool HasUnsupportedTopLevelFields(JsonElement root)
    {
        return root.EnumerateObject()
            .Any(property => property.Name is not ("name" or "parameters"));
    }

    private static bool HasUnsupportedFields(JsonElement element, string[] allowedNames)
    {
        return element.EnumerateObject()
            .Any(property => !allowedNames.Contains(property.Name, StringComparer.Ordinal));
    }

    private static bool ContainsIdentityField(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (IsIdentityField(property.Name) || ContainsIdentityField(property.Value))
                {
                    return true;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ContainsIdentityField(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsIdentityField(string name)
    {
        return string.Equals(name, "userId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "profileId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "localProfileId", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value);
    }

    private static bool TryGetDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDecimal(out value);
    }

    private static bool TryGetDate(JsonElement element, string propertyName, out DateOnly value)
    {
        value = default;
        return TryGetString(element, propertyName, out var text)
            && DateOnly.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
