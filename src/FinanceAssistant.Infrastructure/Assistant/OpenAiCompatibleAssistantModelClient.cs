using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinanceAssistant.Application.Assistant;

namespace FinanceAssistant.Infrastructure.Assistant;

public sealed class OpenAiCompatibleAssistantModelClient : IAssistantModelClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly AssistantModelOptions options;

    public OpenAiCompatibleAssistantModelClient(
        HttpClient httpClient,
        AssistantModelOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public AssistantConfigurationDisclosure GetConfigurationDisclosure()
    {
        var endpoint = GetEndpoint();
        var isRemote = !endpoint.IsLoopback;
        return new AssistantConfigurationDisclosure(
            endpoint,
            string.IsNullOrWhiteSpace(options.Model) ? AssistantModelOptions.DefaultModel : options.Model,
            isRemote,
            options.AllowRemote,
            RequiresRemoteDisclosure: isRemote && options.AllowRemote,
            WarningMessage: isRemote && options.AllowRemote
                ? "Assistant model requests may send financial or extracted document content outside this machine."
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

    private Uri GetEndpoint()
    {
        var endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
            ? AssistantModelOptions.DefaultEndpoint
            : options.Endpoint;

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return new Uri(AssistantModelOptions.DefaultEndpoint);
        }

        return uri;
    }

    private static StringContent JsonContent(AssistantModelRequest request, string model)
    {
        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserMessage },
            },
            tools = request.ToolSchemas.Select(schema => new
            {
                name = schema.Key,
                schema = schema.Value,
            }),
            stream = false,
        };

        return new StringContent(
            JsonSerializer.Serialize(payload, SerializerOptions),
            Encoding.UTF8,
            "application/json");
    }

    private static string? ExtractContent(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var firstChoice = choices.EnumerateArray().FirstOrDefault();
        if (firstChoice.ValueKind != JsonValueKind.Object
            || !firstChoice.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return content.GetString();
    }
}
