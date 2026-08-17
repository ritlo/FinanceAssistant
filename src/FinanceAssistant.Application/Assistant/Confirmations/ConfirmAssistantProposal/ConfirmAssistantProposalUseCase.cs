using System.Text.Json;
using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Application.PersonalRecords.Notes.CreateNote;
using FinanceAssistant.Application.PersonalRecords.Reminders.CreateReminder;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Assistant.Confirmations.ConfirmAssistantProposal;

public sealed class ConfirmAssistantProposalUseCase
{
    private static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICurrentProfileProvider currentProfileProvider;
    private readonly IAssistantConfirmationRepository repository;
    private readonly LogTransactionUseCase logTransaction;
    private readonly CreateNoteUseCase createNote;
    private readonly CreateReminderUseCase createReminder;
    private readonly IClock clock;

    public ConfirmAssistantProposalUseCase(
        ICurrentProfileProvider currentProfileProvider,
        IAssistantConfirmationRepository repository,
        LogTransactionUseCase logTransaction,
        CreateNoteUseCase createNote,
        CreateReminderUseCase createReminder,
        IClock clock)
    {
        this.currentProfileProvider = currentProfileProvider;
        this.repository = repository;
        this.logTransaction = logTransaction;
        this.createNote = createNote;
        this.createReminder = createReminder;
        this.clock = clock;
    }

    public async Task<AssistantConfirmationResult> ExecuteAsync(
        ConfirmAssistantProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var profileId = await currentProfileProvider.GetCurrentProfileIdAsync(cancellationToken);
        var record = await repository.GetByTokenAsync(profileId, request.Token, cancellationToken);

        if (record is null)
        {
            throw new DomainValidationException("Assistant confirmation was not found.");
        }

        if (!string.Equals(record.OperationFingerprint, request.OperationFingerprint, StringComparison.Ordinal))
        {
            throw new DomainValidationException("Assistant confirmation fingerprint did not match.");
        }

        if (record.Status == AssistantConfirmationStatus.Completed)
        {
            return AssistantConfirmationResult.FromRecord(record);
        }

        if (record.Status == AssistantConfirmationStatus.Cancelled)
        {
            throw new DomainValidationException("Assistant confirmation was cancelled.");
        }

        if (record.Status == AssistantConfirmationStatus.Expired || record.ExpiresAt <= clock.UtcNow)
        {
            record.MarkExpired();
            await repository.UpdateAsync(record, CancellationToken.None);
            throw new DomainValidationException("Assistant confirmation expired.");
        }

        var claimed = await repository.TryClaimAsync(profileId, request.Token, cancellationToken);
        if (!claimed)
        {
            var latest = await repository.GetByTokenAsync(profileId, request.Token, cancellationToken);
            if (latest?.Status == AssistantConfirmationStatus.Completed)
            {
                return AssistantConfirmationResult.FromRecord(latest);
            }

            throw new DomainValidationException("Assistant confirmation could not be claimed.");
        }

        record = await repository.GetByTokenAsync(profileId, request.Token, cancellationToken)
            ?? throw new DomainValidationException("Assistant confirmation was not found.");

        var completedResult = await ExecuteProposalAsync(record, cancellationToken);
        record.MarkCompleted(completedResult);
        await repository.UpdateAsync(record, cancellationToken);

        return AssistantConfirmationResult.FromRecord(record);
    }

    private async Task<string> ExecuteProposalAsync(
        AssistantConfirmationRecord record,
        CancellationToken cancellationToken)
    {
        object result = record.ProposalType switch
        {
            AssistantToolNames.ProposeTransaction => await ExecuteTransactionProposalAsync(record, cancellationToken),
            AssistantToolNames.ProposeNote => await ExecuteNoteProposalAsync(record, cancellationToken),
            AssistantToolNames.ProposePaymentReminder => await ExecutePaymentReminderProposalAsync(record, cancellationToken),
            _ => throw new DomainValidationException("Assistant proposal type is not supported."),
        };

        return JsonSerializer.Serialize(result, result.GetType(), ResultJsonOptions);
    }

    private async Task<LogTransactionResult> ExecuteTransactionProposalAsync(
        AssistantConfirmationRecord record,
        CancellationToken cancellationToken)
    {
        var proposal = AssistantProposalSerializer.Deserialize<ProposeTransactionProposal>(record.SerializedProposal);
        if (proposal.Amount <= 0 || string.IsNullOrWhiteSpace(proposal.Description))
        {
            throw new DomainValidationException("Assistant transaction proposal is invalid.");
        }

        var type = Enum.TryParse<TransactionType>(proposal.TransactionType, ignoreCase: true, out var parsedType)
            ? parsedType
            : TransactionType.Expense;

        return await logTransaction.ExecuteAsync(
            new LogTransactionRequest(
                proposal.Amount,
                type,
                proposal.Date,
                proposal.Description),
            cancellationToken);
    }

    private async Task<object> ExecuteNoteProposalAsync(
        AssistantConfirmationRecord record,
        CancellationToken cancellationToken)
    {
        var proposal = AssistantProposalSerializer.Deserialize<ProposeNoteProposal>(record.SerializedProposal);
        if (string.IsNullOrWhiteSpace(proposal.Content))
        {
            throw new DomainValidationException("Assistant note proposal is invalid.");
        }

        return await createNote.ExecuteAsync(new CreateNoteRequest(proposal.Content), cancellationToken);
    }

    private async Task<object> ExecutePaymentReminderProposalAsync(
        AssistantConfirmationRecord record,
        CancellationToken cancellationToken)
    {
        var proposal = AssistantProposalSerializer.Deserialize<ProposePaymentReminderProposal>(record.SerializedProposal);
        if (string.IsNullOrWhiteSpace(proposal.Content) || proposal.DueDate == default)
        {
            throw new DomainValidationException("Assistant payment reminder proposal is invalid.");
        }

        return await createReminder.ExecuteAsync(
            new CreateReminderRequest(proposal.Content, proposal.DueDate),
            cancellationToken);
    }
}
