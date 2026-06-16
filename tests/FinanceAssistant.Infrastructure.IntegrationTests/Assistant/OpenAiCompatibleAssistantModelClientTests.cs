using System.Net;
using System.Text;
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
        Assert.Contains("\"model\":\"local-model\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"role\":\"system\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"role\":\"user\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"tools\"", handler.RequestBody, StringComparison.Ordinal);
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
                [AssistantToolNames.ReadTransactions] = "{}",
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
