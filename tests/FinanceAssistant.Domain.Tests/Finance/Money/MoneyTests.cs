using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Finance;

namespace FinanceAssistant.Domain.Tests.Finance;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void CreateRejectsNonPositiveAmounts(string amountText)
    {
        var amount = decimal.Parse(amountText);

        var exception = Assert.Throws<DomainValidationException>(() => Money.Create(amount));

        Assert.Equal("Amount must be positive.", exception.Message);
    }

    [Fact]
    public void CreateRejectsAmountsWithMoreThanTwoMeaningfulFractionalDigits()
    {
        var exception = Assert.Throws<DomainValidationException>(() => Money.Create(12.345m));

        Assert.Equal("Amount must have at most two fractional digits.", exception.Message);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.23")]
    [InlineData("1.230")]
    public void CreateAcceptsPositiveAmountsWithAtMostTwoMeaningfulFractionalDigits(string amountText)
    {
        var money = Money.Create(decimal.Parse(amountText));

        Assert.Equal(decimal.Round(decimal.Parse(amountText), 2), money.Amount);
    }
}
