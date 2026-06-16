using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Assistant.Confirmations;

public sealed class AssistantConfirmationRecord
{
    private AssistantConfirmationRecord(
        Guid token,
        LocalProfileId profileId,
        string operationFingerprint,
        string proposalType,
        string serializedProposal,
        AssistantConfirmationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? completedResult)
    {
        Token = token;
        ProfileId = profileId;
        OperationFingerprint = operationFingerprint;
        ProposalType = proposalType;
        SerializedProposal = serializedProposal;
        Status = status;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CompletedResult = completedResult;
    }

    public Guid Token { get; }

    public LocalProfileId ProfileId { get; }

    public string OperationFingerprint { get; }

    public string ProposalType { get; }

    public string SerializedProposal { get; }

    public AssistantConfirmationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public string? CompletedResult { get; private set; }

    public static AssistantConfirmationRecord Create(
        LocalProfileId profileId,
        string operationFingerprint,
        string proposalType,
        string serializedProposal,
        DateTimeOffset createdAt,
        TimeSpan timeToLive)
    {
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new DomainValidationException("Assistant confirmation expiry must be in the future.");
        }

        return Rehydrate(
            Guid.NewGuid(),
            profileId,
            operationFingerprint,
            proposalType,
            serializedProposal,
            AssistantConfirmationStatus.Pending,
            createdAt,
            createdAt.Add(timeToLive),
            completedResult: null);
    }

    public static AssistantConfirmationRecord Rehydrate(
        Guid token,
        LocalProfileId profileId,
        string operationFingerprint,
        string proposalType,
        string serializedProposal,
        AssistantConfirmationStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? completedResult)
    {
        if (token == Guid.Empty)
        {
            throw new DomainValidationException("Assistant confirmation token is required.");
        }

        return new AssistantConfirmationRecord(
            token,
            profileId,
            NormalizeRequired(operationFingerprint, "Assistant confirmation fingerprint"),
            NormalizeRequired(proposalType, "Assistant confirmation proposal type"),
            NormalizeRequired(serializedProposal, "Assistant confirmation proposal"),
            status,
            createdAt,
            expiresAt,
            completedResult);
    }

    public void MarkClaimed()
    {
        if (Status != AssistantConfirmationStatus.Pending)
        {
            throw new DomainValidationException("Assistant confirmation is not pending.");
        }

        Status = AssistantConfirmationStatus.Claimed;
    }

    public void MarkCompleted(string completedResult)
    {
        if (Status != AssistantConfirmationStatus.Claimed)
        {
            throw new DomainValidationException("Assistant confirmation must be claimed before completion.");
        }

        Status = AssistantConfirmationStatus.Completed;
        CompletedResult = NormalizeRequired(completedResult, "Assistant confirmation result");
    }

    public void MarkCancelled()
    {
        if (Status != AssistantConfirmationStatus.Pending)
        {
            throw new DomainValidationException("Only pending assistant confirmations can be cancelled.");
        }

        Status = AssistantConfirmationStatus.Cancelled;
    }

    public void MarkExpired()
    {
        if (Status == AssistantConfirmationStatus.Pending)
        {
            Status = AssistantConfirmationStatus.Expired;
        }
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
