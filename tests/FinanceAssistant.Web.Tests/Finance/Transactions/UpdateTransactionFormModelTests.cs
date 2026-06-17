using System.ComponentModel.DataAnnotations;
using FinanceAssistant.Domain.Finance.Transactions;
using FinanceAssistant.Web.Finance.Transactions.UpdateTransaction;

namespace FinanceAssistant.Web.Tests.Finance.Transactions;

public sealed class UpdateTransactionFormModelTests
{
    [Fact]
    public void ToRequestMapsFormFieldsToUseCaseRequest()
    {
        var transactionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var model = new UpdateTransactionFormModel
        {
            Id = transactionId,
            Amount = 56.78m,
            Type = TransactionType.Income,
            Date = new DateOnly(2026, 6, 16),
            Description = "Updated invoice",
            CategoryId = categoryId.ToString(),
        };

        var request = model.ToRequest();

        Assert.Equal(transactionId, request.Id);
        Assert.Equal(56.78m, request.Amount);
        Assert.Equal(TransactionType.Income, request.Type);
        Assert.Equal(new DateOnly(2026, 6, 16), request.Date);
        Assert.Equal("Updated invoice", request.Description);
        Assert.Equal(categoryId, request.CategoryId);
    }

    [Fact]
    public void ToRequestLeavesCategoryEmptyForAutomaticCategorization()
    {
        var model = new UpdateTransactionFormModel
        {
            Id = Guid.NewGuid(),
            Amount = 56.78m,
            Type = TransactionType.Income,
            Date = new DateOnly(2026, 6, 16),
            Description = "Updated invoice",
            CategoryId = string.Empty,
        };

        var request = model.ToRequest();

        Assert.Null(request.CategoryId);
    }

    [Fact]
    public void ValidationPreservesPositiveAmountAndRequiredDescriptionRules()
    {
        var model = new UpdateTransactionFormModel
        {
            Amount = 0m,
            Description = string.Empty,
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateTransactionFormModel.Amount)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(UpdateTransactionFormModel.Description)));
    }

    private static List<ValidationResult> Validate(UpdateTransactionFormModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
