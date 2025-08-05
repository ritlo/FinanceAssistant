using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FinanceTracker.ApiService.Models.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FinanceTracker.ApiService.Services
{
    public class AgentService
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;

        public AgentService(IConfiguration configuration, TransactionService transactionService, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<AgentService>();

            // Configure the local LLM endpoint
            var openAiEndpoint = _configuration["OpenAI:Endpoint"] ?? "http://localhost:8080"; // llama.cpp
            var openAiModelId = _configuration["OpenAI:ModelId"] ?? "gemma-3-27b-it-qat-IQ4_XS.gguf"; // Default model

            _logger.LogInformation("Using OpenAI Endpoint: {Endpoint}", openAiEndpoint);
            _logger.LogInformation("Using OpenAI Model ID: {ModelId}", openAiModelId);

            _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: openAiModelId,
                    apiKey: "1", // No API key needed for local Ollama
                    endpoint: new Uri(openAiEndpoint))
                .Build();

            // Import plugins
            _kernel.ImportPluginFromObject(new FinancialPlugin(transactionService, loggerFactory.CreateLogger<FinancialPlugin>()), "FinancialPlugin");
            _kernel.ImportPluginFromObject(new DocumentParsingPlugin(loggerFactory.CreateLogger<DocumentParsingPlugin>()), "DocumentParsingPlugin");
        }

        public async Task<string> ProcessUserRequestAsync(string userRequest, string userId)
        {
            // Add user ID to the kernel arguments for plugins to access
            var arguments = new KernelArguments
            {
                ["userId"] = userId
            };

            // Enable planning and function calling
            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            try
            {
                _logger.LogInformation("Invoking prompt with user request: {Request}", userRequest);
                var result = await _kernel.InvokePromptAsync(userRequest, arguments);
                _logger.LogInformation("Successfully received response from LLM.");
                return result.GetValue<string>() ?? "I'm sorry, I couldn't process your request.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while communicating with the LLM.");
                return "An error occurred while processing your request. Please try again later.";
            }
        }

        public async IAsyncEnumerable<string> StreamUserRequestAsync(string userRequest, string userId)
        {
            var arguments = new KernelArguments
            {
                ["userId"] = userId
            };

            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            bool errorOccurred = false;
            string errorMessage = string.Empty;
            var results = new List<string>();

            try
            {
                _logger.LogInformation("Streaming prompt with user request: {Request}", userRequest);
                var resultStream = _kernel.InvokePromptStreamingAsync(userRequest, arguments);
                await foreach (var update in resultStream)
                {
                    var content = update.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        results.Add(content);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while streaming from the LLM.");
                errorOccurred = true;
                errorMessage = "[Error] An error occurred while processing your request.";
            }

            foreach (var item in results)
            {
                yield return item;
            }
            if (errorOccurred && errorMessage != null)
            {
                yield return errorMessage;
            }
        }
    }
}
