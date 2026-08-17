namespace FinanceAssistant.Domain.Documents;

public static class DocumentMediaTypes
{
    public const string Pdf = "application/pdf";
    public const string PlainText = "text/plain";

    public static bool IsSupported(string mediaType)
    {
        return string.Equals(mediaType, Pdf, StringComparison.Ordinal)
            || string.Equals(mediaType, PlainText, StringComparison.Ordinal);
    }
}
