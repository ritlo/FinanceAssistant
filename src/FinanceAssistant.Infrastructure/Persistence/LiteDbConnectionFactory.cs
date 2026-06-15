using LiteDB;

namespace FinanceAssistant.Infrastructure.Persistence;

internal sealed class LiteDbConnectionFactory
{
    private readonly FinanceAssistantDataOptions options;

    public LiteDbConnectionFactory(FinanceAssistantDataOptions options)
    {
        this.options = options;
    }

    public LiteDatabase Open()
    {
        return new LiteDatabase(new ConnectionString
        {
            Filename = options.DatabasePath,
            Connection = ConnectionType.Shared,
        });
    }
}
