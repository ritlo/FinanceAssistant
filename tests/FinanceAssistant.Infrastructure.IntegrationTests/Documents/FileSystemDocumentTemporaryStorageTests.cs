using System.Text;
using FinanceAssistant.Infrastructure.Documents;
using FinanceAssistant.Infrastructure.Persistence;

namespace FinanceAssistant.Infrastructure.IntegrationTests.Documents;

public sealed class FileSystemDocumentTemporaryStorageTests
{
    [Fact]
    public async Task SaveHashAndDeleteUsesConfiguredTemporaryDirectory()
    {
        using var directory = TemporaryDirectory.Create();
        var temporaryDirectory = Path.Combine(directory.Path, "document-temp");
        var storage = new FileSystemDocumentTemporaryStorage(new FinanceAssistantDataOptions
        {
            DatabasePath = Path.Combine(directory.Path, "FinanceAssistant.db"),
            DocumentTemporaryDirectoryPath = temporaryDirectory,
            Currency = "USD",
        });

        var temporaryFile = await storage.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("hello")));
        var filesAfterSave = Directory.GetFiles(temporaryDirectory);
        var hash = await storage.ComputeSha256HashAsync(temporaryFile);
        await storage.DeleteAsync(temporaryFile);

        Assert.Equal(5, temporaryFile.ByteLength);
        Assert.Single(filesAfterSave);
        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", hash);
        Assert.Empty(Directory.GetFiles(temporaryDirectory));
    }
}
