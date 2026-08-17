namespace FinanceAssistant.Application.Documents;

public interface IDocumentParser
{
    Task<DocumentParseResult> ParseAsync(
        Stream content,
        string declaredMediaType,
        CancellationToken cancellationToken = default);
}
