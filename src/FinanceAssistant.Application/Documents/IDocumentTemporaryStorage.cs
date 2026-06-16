namespace FinanceAssistant.Application.Documents;

public interface IDocumentTemporaryStorage
{
    Task<TemporaryDocumentFile> SaveAsync(Stream content, CancellationToken cancellationToken = default);

    Task<string> ComputeSha256HashAsync(
        TemporaryDocumentFile temporaryFile,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(TemporaryDocumentFile temporaryFile, CancellationToken cancellationToken = default);
}
