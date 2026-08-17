using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Documents;

public interface IDocumentMetadataRepository
{
    Task AddDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedDocument>> ListDocumentsAsync(
        LocalProfileId profileId,
        CancellationToken cancellationToken = default);

    Task<UploadedDocument?> GetDocumentAsync(
        LocalProfileId profileId,
        DocumentId documentId,
        CancellationToken cancellationToken = default);

    Task UpdateDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default);
}
