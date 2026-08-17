namespace FinanceAssistant.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
