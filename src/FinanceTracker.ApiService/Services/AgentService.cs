using Newtonsoft.Json.Linq;
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
using Newtonsoft.Json;

namespace FinanceTracker.ApiService.Services
{
    public class AgentService
    {
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;
        private readonly string _functionCallingMode;
        private readonly TransactionService _transactionService;

        public AgentService(IConfiguration configuration, TransactionService transactionService, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<AgentService>();
            _transactionService = transactionService;

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

            _functionCallingMode = configuration["FunctionCallingMode"] ?? "native";
        }

        public async Task<string> ProcessUserRequestAsync(string userRequest, string userId)
        {
            // This method is a placeholder. Implement as needed or remove if not used.
            return await HandleLLMRequestAsync(userRequest, userId);
        }
        // Placeholder for native function calling mode. Implement as needed.
        private async Task<string> HandleNativeFunctionCallingAsync(string prompt, string? userId = null)
        {
            // For now, just call the prompt function calling handler.
            return await HandlePromptFunctionCallingAsync(prompt, userId);
        }

        // Helper to strip code block markers from LLM output
        private string StripCodeBlockMarkers(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var trimmed = input.Trim();
            if (trimmed.StartsWith("```"))
            {
                int first = trimmed.IndexOf("```", StringComparison.Ordinal);
                int last = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (first != last)
                {
                    return trimmed.Substring(first + 3, last - first - 3).Trim();
                }
                return trimmed.Substring(3).Trim();
            }
            return trimmed;
        }

        private async Task<string> HandlePromptFunctionCallingAsync(string prompt, string? userId = null)
        {
            string systemPrompt =
                "You are a financial assistant. You can call the following functions if needed. Otherwise, answer in natural language.\n" +
                "\n" +
                "IMPORTANT: When logging a transaction, you MUST always categorize it into ONE of the following categories: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, or Transport. Do not use any other category.\n" +
                "\n" +
                "FUNCTIONS:\n" +
                "[\n" +
                "  {\n" +
                "    \"name\": \"LogTransaction\",\n" +
                "    \"description\": \"Log a financial transaction for the user.\",\n" +
                "    \"parameters\": {\n" +
                "      \"amount\": \"number, required\",\n" +
                "      \"category\": \"string, required. Must be one of: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, Transport.\",\n" +
                "      \"description\": \"string, optional\",\n" +
                "      \"date\": \"string, required, format: YYYY-MM-DD\"\n" +
                "    }\n" +
                "  },\n" +
                "  {\n" +
                "    \"name\": \"ReadTransactions\",\n" +
                "    \"description\": \"Get a list of the user's recent transactions.\",\n" +
                "    \"parameters\": {}\n" +
                "  }\n" +
                "]\n" +
                "\n" +
                "INSTRUCTIONS:\n" +
                "- If the user wants to log a transaction, respond ONLY with a JSON object: {\"name\": \"LogTransaction\", \"parameters\": {...}}\n" +
                "- The \"category\" parameter MUST be one of: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, or Transport.\n" +
                "- If the user wants to see their transactions, respond ONLY with a JSON object: {\"name\": \"ReadTransactions\", \"parameters\": {}}\n" +
                "- For all other queries, answer in natural language.\n" +
                "- Do not use markdown, backticks, or extra text when calling a function.\n" +
                "\n" +
                "EXAMPLES:\n" +
                "User: I spent $5 at taco bell today\n" +
                "Assistant: {\"name\": \"LogTransaction\", \"parameters\": {\"amount\": 5, \"category\": \"Food and Drinks\", \"description\": \"Taco Bell\", \"date\": \"2025-08-08\"}}\n" +
                "User: Show me my recent expenses\n" +
                "Assistant: {\"name\": \"ReadTransactions\", \"parameters\": {}}\n" +
                "User: How can I save more money?\n" +
                "Assistant: To save more money, try creating a budget, tracking your expenses, and setting aside savings first.\n";
            string fullPrompt = $"{systemPrompt}\nUser: {prompt}";
            _logger.LogInformation("[LLM][Prompt] Full prompt sent to LLM:\n{Prompt}", fullPrompt);

            try
            {
                _logger.LogInformation("[LLM] Invoking prompt-engineered function calling with prompt: {Prompt}", prompt);
                var arguments = new KernelArguments
                {
                    ["request"] = fullPrompt
                };

                var result = await _kernel.InvokePromptAsync(fullPrompt, arguments);
                var llmResponse = result.GetValue<string>() ?? "";
                _logger.LogInformation("[LLM] Raw LLM response: {LlmResponse}", llmResponse);

                // Strip code block markers before parsing
                var cleanedResponse = StripCodeBlockMarkers(llmResponse);
                _logger.LogInformation("[LLM] Cleaned LLM response: {CleanedResponse}", cleanedResponse);

                // Try to parse the response as JSON
                FunctionCall? functionCall = null;
                try
                {
                    functionCall = JsonConvert.DeserializeObject<FunctionCall>(cleanedResponse);
                    _logger.LogInformation("[LLM] Parsed function call: {FunctionCall}", functionCall != null ? JsonConvert.SerializeObject(functionCall) : "null");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[LLM] Failed to parse LLM response as JSON: {Response}", cleanedResponse);
                    return "Sorry, I could not understand your request. Please rephrase or try again using clear instructions.";
                }

                if (functionCall != null && !string.IsNullOrEmpty(functionCall.Function))
                {
                    _logger.LogInformation("[LLM] Function call detected: {Function}", functionCall.Function);
                    switch (functionCall.Function)
                    {
                        case "LogTransaction":
                            _logger.LogInformation("[LLM] Attempting to log transaction with parameters: {Parameters}", functionCall.Parameters != null ? functionCall.Parameters.ToString() : "null");
                            // Only return success if the backend logic actually ran
                            return await HandleLogTransaction(functionCall.Parameters, userId);
                        // Add more cases as needed
                        default:
                            _logger.LogWarning("[LLM] Unknown function requested: {Function}", functionCall.Function);
                            return $"Unknown function: {functionCall.Function}";
                    }
                }
                else
                {
                    _logger.LogWarning("[LLM] No valid function call found in LLM response. LLM output: {LlmResponse}", cleanedResponse);
                    // Never return LLM's hallucinated text; always require a valid function call
                    return "Sorry, I could not process your request. Please try again and ensure your request is clear and actionable.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLM] An error occurred during prompt-engineered function calling.");
                return "An error occurred while processing your request. Please try again later.";
            }
        }

        public async IAsyncEnumerable<string> StreamUserRequestAsync(string userRequest, string userId)
        {
            // Use the same Gemma-style system prompt and function definition as non-streaming
            string systemPrompt =
                "You are a financial assistant. You can call the following functions if needed. Otherwise, answer in natural language.\n" +
                "\n" +
                "IMPORTANT: When logging a transaction, you MUST always categorize it into ONE of the following categories: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, or Transport. Do not use any other category.\n" +
                "\n" +
                "FUNCTIONS:\n" +
                "[\n" +
                "  {\n" +
                "    \"name\": \"LogTransaction\",\n" +
                "    \"description\": \"Log a financial transaction for the user.\",\n" +
                "    \"parameters\": {\n" +
                "      \"amount\": \"number, required\",\n" +
                "      \"category\": \"string, required. Must be one of: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, Transport.\",\n" +
                "      \"description\": \"string, optional\",\n" +
                "      \"date\": \"string, required, format: YYYY-MM-DD\"\n" +
                "    }\n" +
                "  },\n" +
                "  {\n" +
                "    \"name\": \"ReadTransactions\",\n" +
                "    \"description\": \"Get a list of the user's recent transactions.\",\n" +
                "    \"parameters\": {}\n" +
                "  }\n" +
                "]\n" +
                "\n" +
                "INSTRUCTIONS:\n" +
                "- If the user wants to log a transaction, respond ONLY with a JSON object: {\"name\": \"LogTransaction\", \"parameters\": {...}}\n" +
                "- The \"category\" parameter MUST be one of: Food and Drinks, Groceries, Shopping, Travel, Services, Entertainment, Health, or Transport.\n" +
                "- If the user wants to see their transactions, respond ONLY with a JSON object: {\"name\": \"ReadTransactions\", \"parameters\": {}}\n" +
                "- For all other queries, answer in natural language.\n" +
                "- Do not use markdown, backticks, or extra text when calling a function.\n" +
                "\n" +
                "EXAMPLES:\n" +
                "User: I spent $5 at taco bell today\n" +
                "Assistant: {\"name\": \"LogTransaction\", \"parameters\": {\"amount\": 5, \"category\": \"Food and Drinks\", \"description\": \"Taco Bell\", \"date\": \"2025-08-08\"}}\n" +
                "User: Show me my recent expenses\n" +
                "Assistant: {\"name\": \"ReadTransactions\", \"parameters\": {}}\n" +
                "User: How can I save more money?\n" +
                "Assistant: To save more money, try creating a budget, tracking your expenses, and setting aside savings first.\n";
            var fullPrompt = $"{systemPrompt}\nUser: {userRequest}";
            _logger.LogInformation("[LLM][Prompt][Stream] Full prompt sent to LLM:\n{Prompt}", fullPrompt);

            var arguments = new KernelArguments
            {
                ["userId"] = userId,
                ["request"] = fullPrompt
            };

            _logger.LogInformation("[LLM][Stream] Streaming prompt with user request: {Request}", userRequest);
            var resultStream = _kernel.InvokePromptStreamingAsync(fullPrompt, arguments);
            string llmResponse = string.Empty;
            await foreach (var update in resultStream)
            {
                llmResponse += update.ToString();
            }

            // After streaming, parse the full response for a function call
            _logger.LogInformation("[LLM][Stream] Raw streamed LLM response: {LlmResponse}", llmResponse);
            // Strip code block markers before parsing
            var cleanedResponse = StripCodeBlockMarkers(llmResponse);
            _logger.LogInformation("[LLM][Stream] Cleaned streamed LLM response: {CleanedResponse}", cleanedResponse);
            FunctionCall? functionCall = null;
            bool parseError = false;
            try
            {
                functionCall = JsonConvert.DeserializeObject<FunctionCall>(cleanedResponse);
                _logger.LogInformation("[LLM][Stream] Parsed function call: {FunctionCall}", functionCall != null ? JsonConvert.SerializeObject(functionCall) : "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLM][Stream] Failed to parse streamed LLM response as JSON: {Response}", cleanedResponse);
                parseError = true;
            }

            if (parseError)
            {
                yield return "Sorry, I could not understand your request. Please rephrase or try again using clear instructions.";
                yield break;
            }

            if (functionCall != null && !string.IsNullOrEmpty(functionCall.Function))
            {
                _logger.LogInformation("[LLM][Stream] Function call detected: {Function}", functionCall.Function);
                switch (functionCall.Function)
                {
                    case "LogTransaction":
                        _logger.LogInformation("[LLM][Stream][DB] Attempting to log transaction with parameters: {Parameters}", functionCall.Parameters != null ? functionCall.Parameters.ToString() : "null");
                        yield return await HandleLogTransaction(functionCall.Parameters, userId);
                        yield break;
                    case "ReadTransactions":
                        _logger.LogInformation("[LLM][Stream][DB] Attempting to read transactions for user: {UserId}", userId);
                        yield return await HandleReadTransactions(userId);
                        yield break;
                    default:
                        _logger.LogWarning("[LLM][Stream] Unknown function requested: {Function}", functionCall.Function);
                        yield return $"Unknown function: {functionCall.Function}";
                        yield break;
                }
            }
            else
            {
                _logger.LogWarning("[LLM][Stream] No valid function call found in streamed LLM response. LLM output: {LlmResponse}", cleanedResponse);
                yield return "Sorry, I could not process your request. Please try again and ensure your request is clear and actionable.";
                yield break;
            }
        // ...existing code...
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




        public async Task<string> HandleLLMRequestAsync(string prompt, string? userId = null)
        {
            if (_functionCallingMode == "native")
            {
                return await HandleNativeFunctionCallingAsync(prompt, userId);
            }
            else if (_functionCallingMode == "prompt")
            {
                return await HandlePromptFunctionCallingAsync(prompt, userId);
            }
            else
            {
                throw new InvalidOperationException("Invalid FunctionCallingMode in configuration.");
            }
        }

        // Helper for deserialization (must be inside AgentService class)
        private class FunctionCall
        {
            [JsonProperty("name")]
            public string? Function { get; set; }
            public JObject? Parameters { get; set; }
        }

        // Handler for LogTransaction: maps parameters and calls the real transaction service
        private async Task<string> HandleLogTransaction(JObject? parameters, string? userIdFromContext = null)
        {
            if (parameters is null)
            {
                _logger.LogWarning("[LLM] LogTransaction called with null parameters.");
                return "No parameters provided for transaction.";
            }
            try
            {
                var amount = parameters["amount"]?.Value<decimal>() ?? 0;
                var category = parameters["category"]?.Value<string>() ?? "Uncategorized";
                var description = parameters["description"]?.Value<string>() ?? string.Empty;
                var date = parameters["date"]?.Value<DateTime?>() ?? DateTime.UtcNow;
                var userId = parameters["userId"]?.Value<string>() ?? string.Empty;

                // Always use the provided userId from context if available
                if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(userIdFromContext))
                {
                    userId = userIdFromContext;
                    _logger.LogInformation("[LLM][DB] Injected userId from context: {UserId}", userId);
                }
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("[LLM][DB] No userId provided for transaction. Defaulting to empty string.");
                }

                _logger.LogInformation("[LLM][DB] Attempting to log transaction: amount={Amount}, category={Category}, description={Description}, date={Date}, userId={UserId}", amount, category, description, date, userId);
                var success = await _transactionService.LogTransactionAsync(amount, category, description, date, userId);
                if (success)
                {
                    return "Transaction logged successfully.";
                }
                else
                {
                    return "Failed to log transaction. Please check your input and try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLM][DB] Failed to log transaction from LLM function call.");
                return "Failed to log transaction. Please check your input and try again.";
            }
        }

        // Handler for ReadTransactions: returns a summary of recent transactions for the user
        private async Task<string> HandleReadTransactions(string userId)
        {
            try
            {
                var transactions = await _transactionService.GetRecentTransactionsAsync(userId);
                if (transactions == null || transactions.Count == 0)
                {
                    return "No transactions found.";
                }
                // Format a simple summary
                var summary = "Recent transactions:\n";
                foreach (var t in transactions)
                {
                    summary += $"- {t.Date:yyyy-MM-dd}: {t.Amount} {t.Category} {t.Description}\n";
                }
                return summary.TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLM][DB] Failed to read transactions for user {UserId}.", userId);
                return "Failed to retrieve transactions. Please try again later.";
            }
        }
    }
}
