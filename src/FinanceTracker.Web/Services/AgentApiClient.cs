using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using FinanceTracker.Web.Services;

namespace FinanceTracker.Web.Services
{
    #region AgentApiClient
    /// <summary>
    /// Provides methods to interact with the Agent API.
    /// </summary>
    /// <inheritdoc cref="IAgentApiClient"/>
    public sealed class AgentApiClient : IAgentApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AgentApiClient> _logger;
        private readonly IConfiguration _configuration;
        private const long MaxUploadFileSize = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentApiClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public AgentApiClient(HttpClient httpClient, ILogger<AgentApiClient> logger, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(configuration);
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        #region Public Methods

        /// <summary>
        /// Processes an agent request and returns the response.
        /// </summary>
        /// <param name="prompt">The prompt to send to the agent.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The agent's response.</returns>
        /// <exception cref="ArgumentException">Thrown if prompt is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown if UserId is not configured.</exception>
        public async Task<AgentResponse> ProcessAgentRequestAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));

            var userId = _configuration["UserId"];
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("UserId configuration is missing. Please set the 'UserId' in your configuration.");

            var request = new AgentRequest
            {
                Prompt = prompt,
                UserId = userId
            };

            try
            {
                _logger.LogInformation("Sending request to API with prompt: {Prompt}", prompt);
                var response = await _httpClient.PostAsJsonAsync("/api/agent/process", request, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API request failed with status code {StatusCode} and content: {ErrorContent}", response.StatusCode, responseContent);
                    return new AgentResponse { Success = false, ErrorMessage = responseContent };
                }
                _logger.LogInformation("Successfully received response from API: {Response}", responseContent);
                return new AgentResponse { Success = true, Content = responseContent };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Agent request was canceled.");
                return new AgentResponse { Success = false, ErrorMessage = "Request was canceled." };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error occurred while sending the request to the API.");
                return new AgentResponse { Success = false, ErrorMessage = $"Error connecting to API: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return new AgentResponse { Success = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }

        /// <summary>
        /// Streams an agent request and invokes callbacks for each chunk or error.
        /// </summary>
        /// <param name="prompt">The prompt to send to the agent.</param>
        /// <param name="onChunk">Callback for each chunk of data.</param>
        /// <param name="onError">Callback for errors.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <remarks>TODO: Consider returning a Task that completes when the stream ends for better testability.</remarks>
        public async Task StreamAgentRequestStreamAsync(string prompt, Action<string> onChunk, Action<string>? onError = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));

            var userId = _configuration["UserId"];
            if (string.IsNullOrWhiteSpace(userId))
            {
                onError?.Invoke("UserId configuration is missing. Please set the 'UserId' in your configuration.");
                return;
            }

            var request = new AgentRequest
            {
                Prompt = prompt,
                UserId = userId
            };

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agent/stream-process")
                {
                    Content = JsonContent.Create(request)
                };
                using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogError("Streaming API request failed with status code {StatusCode} and content: {ErrorContent}", response.StatusCode, errorContent);
                    onError?.Invoke($"Error from API: {errorContent}");
                    return;
                }
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("data: "))
                    {
                        var chunk = line.Substring(6);
                        onChunk(chunk);
                    }
                    else if (line.StartsWith("event: error"))
                    {
                        var error = await reader.ReadLineAsync().ConfigureAwait(false);
                        onError?.Invoke(error?.Replace("data: ", "") ?? "Unknown error");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Streaming agent request was canceled.");
                onError?.Invoke("Streaming request was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while streaming the request to the API.");
                onError?.Invoke($"Streaming error: {ex.Message}");
            }
        }

        /// <summary>
        /// Uploads a document to the agent API.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the upload operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if file is null.</exception>
        public async Task<AgentResponse> UploadDocumentAsync(IBrowserFile file, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Size > MaxUploadFileSize)
                return new AgentResponse { Success = false, ErrorMessage = $"File size exceeds the maximum allowed size of {MaxUploadFileSize / (1024 * 1024)} MB." };

            var userId = _configuration["UserId"];
            if (string.IsNullOrWhiteSpace(userId))
                return new AgentResponse { Success = false, ErrorMessage = "UserId configuration is missing. Please set the 'UserId' in your configuration." };

            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream(MaxUploadFileSize, cancellationToken);
                content.Add(new StreamContent(fileStream), "file", file.Name);
                content.Add(new StringContent(userId), "userId");

                var response = await _httpClient.PostAsync("/api/agent/upload", content, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("File upload failed with status code {StatusCode} and content: {ErrorContent}", response.StatusCode, responseContent);
                    return new AgentResponse { Success = false, ErrorMessage = responseContent };
                }
                _logger.LogInformation("File uploaded successfully.");
                return new AgentResponse { Success = true, Content = responseContent };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("File upload was canceled.");
                return new AgentResponse { Success = false, ErrorMessage = "File upload was canceled." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the file.");
                return new AgentResponse { Success = false, ErrorMessage = $"An error occurred while uploading the file: {ex.Message}" };
            }
        }

        #endregion
    }
    #endregion

    // AgentRequest and AgentResponse moved to their own files for maintainability.
}
