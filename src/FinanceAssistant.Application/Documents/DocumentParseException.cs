namespace FinanceAssistant.Application.Documents;

public sealed class DocumentParseException : Exception
{
    public DocumentParseException(string message)
        : base(message)
    {
    }
}
