using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Application.Assistant.Settings;

namespace FinanceAssistant.Infrastructure.Assistant;

public sealed class OpenAiCompatibleAssistantModelClient : IAssistantModelClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly AssistantModelOptions options;
    private readonly IAssistantSettingsRepository settingsRepository;

    public OpenAiCompatibleAssistantModelClient(
        HttpClient httpClient,
        AssistantModelOptions options,
        IAssistantSettingsRepository settingsRepository)
    {
        this.httpClient = httpClient;
        this.options = options;
        this.settingsRepository = settingsRepository;
    }

    public AssistantConfigurationDisclosure GetConfigurationDisclosure()
    {
        var settings = settingsRepository.Get();
        var endpoint = settings.BuildEndpointUri();
        var isRemote = !endpoint.IsLoopback;
        return new AssistantConfigurationDisclosure(
            endpoint,
            string.IsNullOrWhiteSpace(options.Model) ? AssistantModelOptions.DefaultModel : options.Model,
            isRemote,
            settings.AllowRemoteEndpoint,
            RequiresRemoteDisclosure: isRemote,
            WarningMessage: isRemote
                ? settings.AllowRemoteEndpoint
                    ? "Assistant model requests may send financial or extracted document content outside this machine."
                    : "Remote assistant endpoint is configured but blocked until remote access is explicitly allowed."
                : null);
    }

    public async Task<AssistantModelResponse> CompleteAsync(
        AssistantModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var disclosure = GetConfigurationDisclosure();
        if (disclosure.IsRemoteEndpoint && !disclosure.IsRemoteAllowed)
        {
            return AssistantModelResponse.Unavailable(
                "Remote assistant endpoint is not allowed by configuration.",
                disclosure);
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, disclosure.Endpoint)
            {
                Content = JsonContent(request, disclosure.Model),
            };

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AssistantModelResponse.Unavailable(
                    "Assistant endpoint returned an unsuccessful response.",
                    disclosure);
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            var content = ExtractContent(document.RootElement);
            if (string.IsNullOrWhiteSpace(content))
            {
                return AssistantModelResponse.Unavailable(
                    "Assistant endpoint returned an empty response.",
                    disclosure);
            }

            return AssistantModelResponse.Available(content, disclosure);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return AssistantModelResponse.Unavailable(
                "Assistant endpoint is unavailable.",
                disclosure);
        }
    }

    private static StringContent JsonContent(AssistantModelRequest request, string model)
    {
        var payload = new
        {
            model,
            messages = CreateMessages(request),
            tools = request.ToolSchemas.Select(CreateToolDefinition).ToArray(),
            stream = false,
        };

        return new StringContent(
            JsonSerializer.Serialize(payload, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    private static object[] CreateMessages(AssistantModelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RuntimeContext))
        {
            return
            [
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserMessage },
            ];
        }

        return
        [
            new { role = "system", content = request.SystemPrompt },
            new { role = "system", content = request.RuntimeContext },
            new { role = "user", content = request.UserMessage },
        ];
    }

    private static OpenAiToolDefinition CreateToolDefinition(KeyValuePair<string, string> schema)
    {
        using var document = JsonDocument.Parse(schema.Value);
        var root = document.RootElement;

        var description = root.TryGetProperty("description", out var descriptionElement)
            && descriptionElement.ValueKind == JsonValueKind.String
            ? descriptionElement.GetString() ?? string.Empty
            : string.Empty;

        var parameters = root.TryGetProperty("parameters", out var parametersElement)
            && parametersElement.ValueKind == JsonValueKind.Object
            ? parametersElement.Clone()
            : EmptyJsonObject();

        return new OpenAiToolDefinition(
            "function",
            new OpenAiFunctionDefinition(schema.Key, description, parameters));
    }

    private static JsonElement EmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    private static string? ExtractContent(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var firstChoice = choices.EnumerateArray().FirstOrDefault();
        if (firstChoice.ValueKind != JsonValueKind.Object
            || !firstChoice.TryGetProperty("message", out var message))
        {
            return null;
        }

        if (message.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(content.GetString()))
        {
            return content.GetString();
        }

        return ExtractToolCallContent(message);
    }

    private static string? ExtractToolCallContent(JsonElement message)
    {
        if (!message.TryGetProperty("tool_calls", out var toolCalls)
            || toolCalls.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var firstToolCall = toolCalls.EnumerateArray().FirstOrDefault();
        if (firstToolCall.ValueKind != JsonValueKind.Object
            || !firstToolCall.TryGetProperty("function", out var function)
            || !function.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(nameElement.GetString()))
        {
            return null;
        }

        var parameters = ExtractToolCallArguments(function);
        if (parameters.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            new AssistantToolInvocationContent(nameElement.GetString()!, parameters),
            SerializerOptions);
    }

    private static JsonElement ExtractToolCallArguments(JsonElement function)
    {
        if (!function.TryGetProperty("arguments", out var arguments))
        {
            return EmptyJsonObject();
        }

        if (arguments.ValueKind == JsonValueKind.Object)
        {
            return arguments.Clone();
        }

        if (arguments.ValueKind != JsonValueKind.String)
        {
            return default;
        }

        var argumentText = arguments.GetString();
        if (string.IsNullOrWhiteSpace(argumentText))
        {
            return EmptyJsonObject();
        }

        try
        {
            using var document = JsonDocument.Parse(argumentText);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.Clone()
                : default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private sealed record OpenAiToolDefinition(
        string Type,
        OpenAiFunctionDefinition Function);

    private sealed record OpenAiFunctionDefinition(
        string Name,
        string Description,
        JsonElement Parameters);

    private sealed record AssistantToolInvocationContent(
        string Name,
        JsonElement Parameters);
}
