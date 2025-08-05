using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;

namespace FinanceTracker.Web.Services
{
    public class AgentApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentApiClient> _logger;

        public AgentApiClient(HttpClient httpClient, ILogger<AgentApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> ProcessAgentRequest(string prompt)
        {
            try
            {
                _logger.LogInformation("Sending request to API with prompt: {Prompt}", prompt);

                var response = await _httpClient.PostAsJsonAsync("/api/agent/process", prompt);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API request failed with status code {StatusCode} and content: {ErrorContent}", response.StatusCode, errorContent);
                    return $"Error from API: {errorContent}";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully received response from API: {Response}", responseContent);
                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error occurred while sending the request to the API.");
                return $"Error connecting to API: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return $"An unexpected error occurred: {ex.Message}";
            }
        }

        public async Task StreamAgentRequestStream(string prompt, Action<string> onChunk, Action<string>? onError = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/stream-process")
                {
                    Content = JsonContent.Create(prompt)
                };
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Streaming API request failed with status code {StatusCode} and content: {ErrorContent}", response.StatusCode, errorContent);
                    onError?.Invoke($"Error from API: {errorContent}");
                    return;
                }
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("data: "))
                    {
                        var chunk = line.Substring(6);
                        onChunk(chunk);
                    }
                    else if (line.StartsWith("event: error"))
                    {
                        var error = await reader.ReadLineAsync();
                        onError?.Invoke(error?.Replace("data: ", "") ?? "Unknown error");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while streaming the request to the API.");
                onError?.Invoke($"Streaming error: {ex.Message}");
            }
        }
    }
}
