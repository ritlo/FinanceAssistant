using System.Net;
using System.Text;
using System.Text.Json;
using FinanceAssistant.Application.Assistant;
using FinanceAssistant.Infrastructure.Assistant;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Assistant;

public sealed class OpenAiCompatibleAssistantModelClientTests
{
    [Fact]
    public async Task LocalEndpointSendsOpenAiCompatibleRequestShape()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"name\":\"ReadTransactions\",\"parameters\":{}}"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var client = CreateClient(handler, new AssistantModelOptions
        {
            Endpoint = "http://localhost:11434/v1/chat/completions",
            Model = "local-model",
        });

        var result = await client.CompleteAsync(SampleRequest());

        Assert.True(result.IsAvailable);
        Assert.Equal("{\"name\":\"ReadTransactions\",\"parameters\":{}}", result.Content);
        Assert.NotNull(handler.RequestBody);
        using var document = JsonDocument.Parse(handler.RequestBody);
        var root = document.RootElement;
        Assert.Equal("local-model", root.GetProperty("model").GetString());
        Assert.Contains(root.GetProperty("messages").EnumerateArray(), message => message.GetProperty("role").GetString() == "system");
        Assert.Contains(root.GetProperty("messages").EnumerateArray(), message => message.GetProperty("role").GetString() == "user");

        var tool = Assert.Single(root.GetProperty("tools").EnumerateArray());
        Assert.Equal("function", tool.GetProperty("type").GetString());
        var function = tool.GetProperty("function");
        Assert.Equal(AssistantToolNames.ReadTransactions, function.GetProperty("name").GetString());
        Assert.Equal("Read transactions for the server-resolved local profile.", function.GetProperty("description").GetString());
        Assert.Equal(JsonValueKind.Object, function.GetProperty("parameters").ValueKind);
        Assert.False(function.TryGetProperty("kind", out _));
    }

    [Fact]
    public async Task ToolCallResponseIsConvertedToParserInputJson()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "tool_calls": [
                              {
                                "type": "function",
                                "function": {
                                  "name": "ReadTransactions",
                                  "arguments": "{}"
                                }
                              }
                            ]
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var client = CreateClient(handler, new AssistantModelOptions());

        var result = await client.CompleteAsync(SampleRequest());

        Assert.True(result.IsAvailable);
        Assert.Equal("{\"name\":\"ReadTransactions\",\"parameters\":{}}", result.Content);
    }

    [Fact]
    public async Task UnreachableEndpointReturnsUnavailableResult()
    {
        var handler = new ThrowingHandler(new HttpRequestException("connection refused"));
        var client = CreateClient(handler, new AssistantModelOptions());

        var result = await client.CompleteAsync(SampleRequest());

        Assert.False(result.IsAvailable);
        Assert.Equal("Assistant endpoint is unavailable.", result.ErrorMessage);
    }

    [Fact]
    public async Task RemoteEndpointIsBlockedWithoutExplicitAllow()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler, new AssistantModelOptions
        {
            Endpoint = "https://models.example.test/v1/chat/completions",
            AllowRemote = false,
        });

        var result = await client.CompleteAsync(SampleRequest());

        Assert.False(result.IsAvailable);
        Assert.Equal("Remote assistant endpoint is not allowed by configuration.", result.ErrorMessage);
        Assert.True(result.Configuration.IsRemoteEndpoint);
        Assert.False(result.Configuration.IsRemoteAllowed);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task RemoteEndpointIsAllowedOnlyWithDisclosureConfiguration()
    {
        var handler = new RecordingHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"name\":\"GetNotes\",\"parameters\":{}}"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var client = CreateClient(handler, new AssistantModelOptions
        {
            Endpoint = "https://models.example.test/v1/chat/completions",
            AllowRemote = true,
        });

        var result = await client.CompleteAsync(SampleRequest());

        Assert.True(result.IsAvailable);
        Assert.True(result.Configuration.IsRemoteEndpoint);
        Assert.True(result.Configuration.IsRemoteAllowed);
        Assert.True(result.Configuration.RequiresRemoteDisclosure);
        Assert.NotNull(result.Configuration.WarningMessage);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public void DefaultConfigurationUsesLocalEndpoint()
    {
        var client = CreateClient(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)), new AssistantModelOptions());

        var disclosure = client.GetConfigurationDisclosure();

        Assert.Equal(new Uri(AssistantModelOptions.DefaultEndpoint), disclosure.Endpoint);
        Assert.False(disclosure.IsRemoteEndpoint);
        Assert.False(disclosure.RequiresRemoteDisclosure);
    }

    private static OpenAiCompatibleAssistantModelClient CreateClient(
        HttpMessageHandler handler,
        AssistantModelOptions options)
    {
        return new OpenAiCompatibleAssistantModelClient(new HttpClient(handler), options);
    }

    private static AssistantModelRequest SampleRequest()
    {
        return new AssistantModelRequest(
            "system prompt",
            "show my data",
            new Dictionary<string, string>
            {
                [AssistantToolNames.ReadTransactions] =
                    """
                    {
                      "name": "ReadTransactions",
                      "kind": "read",
                      "description": "Read transactions for the server-resolved local profile.",
                      "parameters": {
                        "type": "object",
                        "additionalProperties": false,
                        "properties": {}
                      }
                    }
                    """,
            });
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public RecordingHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        public bool WasCalled { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception exception;

        public ThrowingHandler(Exception exception)
        {
            this.exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw exception;
        }
    }
}
