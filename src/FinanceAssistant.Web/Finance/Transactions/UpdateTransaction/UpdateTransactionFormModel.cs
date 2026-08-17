using System.ComponentModel.DataAnnotations;
using FinanceAssistant.Application.Finance.Transactions.UpdateTransaction;
using FinanceAssistant.Domain.Finance.Transactions;

namespace FinanceAssistant.Web.Finance.Transactions.UpdateTransaction;

public sealed class UpdateTransactionFormModel
{
    public Guid Id { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; } = 0.01m;

    public TransactionType Type { get; set; } = TransactionType.Expense;

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? CategoryId { get; set; }

    public UpdateTransactionRequest ToRequest()
    {
        return new UpdateTransactionRequest(
            Id,
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
