namespace FinanceAssistant.Domain.Common;

public sealed class DomainValidationException : InvalidOperationException
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
