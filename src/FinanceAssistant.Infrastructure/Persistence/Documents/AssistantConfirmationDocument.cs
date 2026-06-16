using FinanceAssistant.Application.Assistant.Confirmations;
using FinanceAssistant.Domain.Identity;
using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence.Documents;

public sealed class AssistantConfirmationDocument
{
    [BsonId]
    public Guid Token { get; set; }

    public Guid ProfileId { get; set; }

    public string OperationFingerprint { get; set; } = string.Empty;

    public string ProposalType { get; set; } = string.Empty;

    public string SerializedProposal { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public string? CompletedResult { get; set; }

    public static AssistantConfirmationDocument FromRecord(AssistantConfirmationRecord record)
    {
        return new AssistantConfirmationDocument
        {
            Token = record.Token,
            ProfileId = record.ProfileId.Value,
            OperationFingerprint = record.OperationFingerprint,
            ProposalType = record.ProposalType,
            SerializedProposal = record.SerializedProposal,
            Status = record.Status.ToString(),
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt,
            CompletedResult = record.CompletedResult,
        };
    }

    public AssistantConfirmationRecord ToRecord()
    {
        return AssistantConfirmationRecord.Rehydrate(
            Token,
            new LocalProfileId(ProfileId),
            OperationFingerprint,
            ProposalType,
            SerializedProposal,
            Enum.Parse<AssistantConfirmationStatus>(Status, ignoreCase: false),
            CreatedAt,
            ExpiresAt,
            CompletedResult);
    }
}
