using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Documents;

public interface IDocumentParsedContentRepository
{
    Task SaveParsedDocumentAsync(ParsedDocument parsedDocument, CancellationToken cancellationToken = default);

    Task<ParsedDocument?> GetParsedDocumentAsync(
        LocalProfileId profileId,
        DocumentId documentId,
        CancellationToken cancellationToken = default);
}
