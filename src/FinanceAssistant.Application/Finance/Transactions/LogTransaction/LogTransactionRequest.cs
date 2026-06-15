using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Transactions.LogTransaction;

public sealed record LogTransactionRequest(
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    string Description,
    Guid? CategoryId = null);
