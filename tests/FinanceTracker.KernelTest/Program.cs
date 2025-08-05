using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration.Json;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Kernel Test...");

        try
        {
            // Load configuration from FinanceTracker.ApiService appsettings.json
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..","..","..", "..", "..", "src", "FinanceTracker.ApiService"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            var openAiEndpoint = config.GetSection("OpenAI:Endpoint").Value ?? "http://localhost:8080";
            var openAiModelId = config.GetSection("OpenAI:ModelId").Value ?? "gemma-3-27b-it-qat-IQ4_XS.gguf";

            Console.WriteLine($"Using Endpoint: {openAiEndpoint}");
            Console.WriteLine($"Using Model ID: {openAiModelId}");

            // Build the kernel
            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: openAiModelId,
                    apiKey: "1", // No API key needed for local Ollama
                    endpoint: new Uri(openAiEndpoint))
                .Build();

            Console.WriteLine("Kernel built successfully.");

            // Test the connection with a simple prompt
            Console.WriteLine("Sending prompt to the model...");
            var result = await kernel.InvokePromptAsync("Hello, are you there?");

            Console.WriteLine("Model Response:");
            Console.WriteLine(result.GetValue<string>());
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nAn error occurred during the test:");
            Console.WriteLine(ex.ToString());
            Console.ResetColor();
        }
        finally
        {
            Console.WriteLine("\nTest finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
