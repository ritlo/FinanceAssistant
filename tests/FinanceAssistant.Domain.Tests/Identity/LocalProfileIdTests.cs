using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Domain.Tests.Identity;

public sealed class LocalProfileIdTests
{
    [Fact]
    public void ConstructorRejectsEmptyGuid()
    {
        var exception = Assert.Throws<DomainValidationException>(() => new LocalProfileId(Guid.Empty));

        Assert.Equal("Local profile ID is required.", exception.Message);
    }

    [Fact]
    public void NewCreatesNonEmptyId()
    {
        var profileId = LocalProfileId.New();

        Assert.NotEqual(Guid.Empty, profileId.Value);
    }
}
