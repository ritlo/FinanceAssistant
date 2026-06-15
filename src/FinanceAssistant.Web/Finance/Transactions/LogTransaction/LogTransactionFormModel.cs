using System.ComponentModel.DataAnnotations;
using FinanceAssistant.Application.Finance.Transactions.LogTransaction;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Web.Finance.Transactions.LogTransaction;

public sealed class LogTransactionFormModel
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; } = 0.01m;

    public TransactionType Type { get; set; } = TransactionType.Expense;

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? CategoryId { get; set; }

    public LogTransactionRequest ToRequest()
    {
        return new LogTransactionRequest(
            Amount,
            Type,
            Date,
            Description,
            TryParseCategoryId());
    }

    private Guid? TryParseCategoryId()
    {
        return Guid.TryParse(CategoryId, out var categoryId) ? categoryId : null;
    }
}
