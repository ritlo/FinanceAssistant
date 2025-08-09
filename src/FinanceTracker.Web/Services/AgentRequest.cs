namespace FinanceTracker.Web.Services
{
    /// <summary>
    /// Represents a request to the agent API.
    /// </summary>
    public sealed class AgentRequest
    {
        public string? Prompt { get; set; }
        public string? UserId { get; set; }
    }
}
