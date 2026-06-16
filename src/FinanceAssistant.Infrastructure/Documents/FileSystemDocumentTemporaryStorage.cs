using System.Security.Cryptography;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.Documents;

public sealed class FileSystemDocumentTemporaryStorage : IDocumentTemporaryStorage
{
    private readonly string rootPath;

    public FileSystemDocumentTemporaryStorage(FinanceAssistantDataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DocumentTemporaryDirectoryPath))
        {
            throw new InvalidOperationException("FinanceAssistant document temporary directory path is required.");
        }

        rootPath = Path.GetFullPath(options.DocumentTemporaryDirectoryPath);
    }

    public async Task<TemporaryDocumentFile> SaveAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(rootPath);

        var id = Guid.NewGuid();
        var path = GetPath(id);
        await using var destination = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(destination, cancellationToken);

        return new TemporaryDocumentFile(id, destination.Length);
    }

    public async Task<string> ComputeSha256HashAsync(
        TemporaryDocumentFile temporaryFile,
        CancellationToken cancellationToken = default)
    {
        await using var source = new FileStream(
            GetPath(temporaryFile.Id),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        var hash = await SHA256.HashDataAsync(source, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public Task<Stream> OpenReadAsync(
        TemporaryDocumentFile temporaryFile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stream source = new FileStream(
            GetPath(temporaryFile.Id),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(source);
    }

    public Task DeleteAsync(TemporaryDocumentFile temporaryFile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = GetPath(temporaryFile.Id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(Guid id)
    {
        return Path.Combine(rootPath, $"{id:N}.upload");
    }
}
