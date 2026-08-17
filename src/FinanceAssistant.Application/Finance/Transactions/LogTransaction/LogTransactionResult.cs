using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Transactions.LogTransaction;

public sealed record LogTransactionResult(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    string Description,
    Guid CategoryId);
