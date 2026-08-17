namespace FinanceAssistant.Application.Assistant.ProcessMessage;

public sealed record ProcessAssistantMessageRequest(
    string Message,
    int? ContextYear = null,
    int? ContextMonth = null,
    string ConversationHistory = "");
