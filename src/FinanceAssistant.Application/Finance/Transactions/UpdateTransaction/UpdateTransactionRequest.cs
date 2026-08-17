using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;

public sealed record UpdateTransactionRequest(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    DateOnly Date,
    string Description,
    Guid? CategoryId = null);
