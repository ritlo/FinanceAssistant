using FinanceAssistant.Application.Common;

namespace FinanceAssistant.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
