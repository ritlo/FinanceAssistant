using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using FinanceTracker.ApiService.Models.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using FinanceTracker.ApiService.Data;

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
            var arguments = new KernelArguments
            {
                ["userId"] = userId
            };

            // Enable planning and function calling
            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            var systemPrompt = $"""
                You are a financial assistant. Your goal is to help users with their finances.
                You can record transactions, retrieve transaction history, and provide financial summaries.
                Be concise and clear in your responses.
                The current date is {DateTime.UtcNow:yyyy-MM-dd}.
                """;

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userRequest);

            arguments["request"] = userRequest;

            try
            {
                _logger.LogInformation("Invoking prompt with user request: {Request}", userRequest);
                var result = await _kernel.InvokePromptAsync(systemPrompt + "\nUser: {{$request}}", arguments);
                _logger.LogInformation("Successfully received response from LLM.");

                // The response from the LLM might include a tool call to the FinancialPlugin.
                // The kernel will automatically invoke the tool and the result will be in the content.
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
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            var systemPrompt = $"""
                You are a financial assistant. Your goal is to help users with their finances.
                You can record transactions, retrieve transaction history, and provide financial summaries.
                Be concise and clear in your responses.
                The current date is {DateTime.UtcNow:yyyy-MM-dd}.
                """;

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userRequest);

            arguments["request"] = userRequest;

            _logger.LogInformation("Streaming prompt with user request: {Request}", userRequest);
            var resultStream = _kernel.InvokePromptStreamingAsync(systemPrompt + "\nUser: {{$request}}", arguments);
            await foreach (var update in resultStream)
            {
                yield return update.ToString();
            }
        }

        public async Task ProcessDocumentStreamAsync(Stream documentStream, string userId, string fileName)
        {
            _logger.LogInformation("Processing document {FileName} for user {UserId}", fileName, userId);

            var arguments = new KernelArguments
            {
                ["userId"] = userId,
                ["documentStream"] = documentStream,
                ["fileName"] = fileName
            };

            try
            {
                var result = await _kernel.InvokeAsync("DocumentParsingPlugin", "ParseDocument", arguments);
                _logger.LogInformation("Document {FileName} processed successfully.", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing document {FileName}.", fileName);
                throw;
            }
        }
    }
}
