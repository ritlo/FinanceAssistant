using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FinanceAssistant.Application.Assistant.Confirmations;

internal static class AssistantProposalSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(object proposal)
    {
        return JsonSerializer.Serialize(proposal, proposal.GetType(), Options);
    }

    public static T Deserialize<T>(string serializedProposal)
    {
        return JsonSerializer.Deserialize<T>(serializedProposal, Options)
            ?? throw new InvalidOperationException("Assistant proposal could not be deserialized.");
    }

    public static string Fingerprint(string proposalType, string serializedProposal)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{proposalType}:{serializedProposal}"));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
