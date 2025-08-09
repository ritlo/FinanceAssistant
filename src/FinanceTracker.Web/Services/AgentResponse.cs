namespace FinanceTracker.Web.Services
{
    /// <summary>
    /// Represents a response from the agent API.
    /// </summary>
    public sealed class AgentResponse
    {
        public bool Success { get; set; }
        public string? Content { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
