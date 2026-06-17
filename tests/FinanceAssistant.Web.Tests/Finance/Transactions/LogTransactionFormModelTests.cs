using System.ComponentModel.DataAnnotations;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Web.Finance.Transactions.LogTransaction;

namespace FinanceAssistant.Web.Tests.Finance.Transactions;

public sealed class LogTransactionFormModelTests
{
    [Fact]
    public void ToRequestMapsFormFieldsToUseCaseRequest()
    {
        var categoryId = Guid.NewGuid();
        var model = new LogTransactionFormModel
        {
            Amount = 12.34m,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 6, 15),
            Description = "Corner shop",
            CategoryId = categoryId.ToString(),
        };

        var request = model.ToRequest();

        Assert.Equal(12.34m, request.Amount);
        Assert.Equal(TransactionType.Expense, request.Type);
        Assert.Equal(new DateOnly(2026, 6, 15), request.Date);
        Assert.Equal("Corner shop", request.Description);
        Assert.Equal(categoryId, request.CategoryId);
    }

    [Fact]
    public void ToRequestLeavesCategoryEmptyForAutomaticCategorization()
    {
        var model = new LogTransactionFormModel
        {
            Amount = 12.34m,
            Type = TransactionType.Expense,
            Date = new DateOnly(2026, 6, 15),
            Description = "Corner shop",
            CategoryId = string.Empty,
        };

        var request = model.ToRequest();

        Assert.Null(request.CategoryId);
    }

    [Fact]
    public void ValidationPreservesPositiveAmountAndRequiredDescriptionRules()
    {
        var model = new LogTransactionFormModel
        {
            Amount = 0m,
            Description = string.Empty,
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LogTransactionFormModel.Amount)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(LogTransactionFormModel.Description)));
    }

    private static List<ValidationResult> Validate(LogTransactionFormModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
