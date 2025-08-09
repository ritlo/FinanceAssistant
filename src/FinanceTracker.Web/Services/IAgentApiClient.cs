using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace FinanceTracker.Web.Services
{
    /// <summary>
    /// Interface for AgentApiClient, provides methods to interact with the Agent API.
    /// </summary>
    public interface IAgentApiClient
    {
        Task<AgentResponse> ProcessAgentRequestAsync(string prompt, CancellationToken cancellationToken = default);
        Task StreamAgentRequestStreamAsync(string prompt, Action<string> onChunk, Action<string>? onError = null, CancellationToken cancellationToken = default);
        Task<AgentResponse> UploadDocumentAsync(IBrowserFile file, CancellationToken cancellationToken = default);
    }
}
