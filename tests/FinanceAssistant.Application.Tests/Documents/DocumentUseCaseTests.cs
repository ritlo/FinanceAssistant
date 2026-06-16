using System.Text;
using FinanceAssistant.Application.Common;
using FinanceAssistant.Application.Documents;
using FinanceAssistant.Application.Documents.CreateDocumentRecord;
using FinanceAssistant.Application.Documents.ListDocuments;
using FinanceAssistant.Application.Documents.UpdateDocumentStatus;
using FinanceAssistant.Application.Identity;
using FinanceAssistant.Domain.Common;
using FinanceAssistant.Domain.Documents;
using FinanceAssistant.Domain.Identity;

namespace FinanceAssistant.Application.Tests.Documents;

public sealed class DocumentUseCaseTests
{
    private const string ValidHash = "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824";

    [Fact]
    public async Task CreateStoresMetadataFromTemporaryContentAndDeletesTemporaryFile()
    {
        var profileId = LocalProfileId.New();
        var createdAt = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeDocumentRepository();
        var temporaryStorage = new FakeDocumentTemporaryStorage(ValidHash, byteLength: 5);
        var useCase = new CreateDocumentRecordUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository,
            temporaryStorage,
            new FixedClock(createdAt));

        var result = await useCase.ExecuteAsync(new CreateDocumentRecordRequest(
            @"C:\uploads\statement.pdf",
            DocumentMediaTypes.Pdf,
            new MemoryStream(Encoding.UTF8.GetBytes("hello"))));

        var document = repository.Documents.Single();
        Assert.Equal(profileId, document.ProfileId);
        Assert.Equal("statement.pdf", document.OriginalDisplayName);
        Assert.Equal(DocumentParseStatus.Pending, result.ParseStatus);
        Assert.Equal(ValidHash, result.Sha256Hash);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(temporaryStorage.SavedFile, temporaryStorage.DeletedFile);
    }

    [Fact]
    public async Task CreateDeletesTemporaryFileWhenPersistenceFails()
    {
        var repository = new FakeDocumentRepository { ThrowOnAdd = true };
        var temporaryStorage = new FakeDocumentTemporaryStorage(ValidHash, byteLength: 5);
        var useCase = new CreateDocumentRecordUseCase(
            new FixedCurrentProfileProvider(LocalProfileId.New()),
            repository,
            temporaryStorage,
            new FixedClock(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(new CreateDocumentRecordRequest(
                "statement.pdf",
                DocumentMediaTypes.Pdf,
                new MemoryStream(Encoding.UTF8.GetBytes("hello")))));

        Assert.Equal(temporaryStorage.SavedFile, temporaryStorage.DeletedFile);
    }

    [Fact]
    public async Task ListSortsNewestFirstAndExcludesOtherProfiles()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var repository = new FakeDocumentRepository();
        var older = repository.AddExisting(
            profileId,
            "older.txt",
            new DateTimeOffset(2026, 6, 15, 9, 0, 0, TimeSpan.Zero));
        var newer = repository.AddExisting(
            profileId,
            "newer.txt",
            new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.Zero));
        repository.AddExisting(otherProfileId, "other.txt", DateTimeOffset.UtcNow);
        var useCase = new ListDocumentsUseCase(new FixedCurrentProfileProvider(profileId), repository);

        var result = await useCase.ExecuteAsync();

        Assert.Collection(
            result,
            document => Assert.Equal(newer.Id.Value, document.Id),
            document => Assert.Equal(older.Id.Value, document.Id));
    }

    [Fact]
    public async Task UpdateStatusUsesCurrentProfileAndClock()
    {
        var profileId = LocalProfileId.New();
        var otherProfileId = LocalProfileId.New();
        var updatedAt = new DateTimeOffset(2026, 6, 16, 12, 30, 0, TimeSpan.Zero);
        var repository = new FakeDocumentRepository();
        var current = repository.AddExisting(profileId, "current.pdf", DateTimeOffset.UtcNow);
        var other = repository.AddExisting(otherProfileId, "other.pdf", DateTimeOffset.UtcNow);
        var useCase = new UpdateDocumentStatusUseCase(
            new FixedCurrentProfileProvider(profileId),
            repository,
            new FixedClock(updatedAt));

        var failed = await useCase.ExecuteAsync(new UpdateDocumentStatusRequest(
            current.Id.Value,
            DocumentParseStatus.Failed,
            "Parser failed"));
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => useCase.ExecuteAsync(new UpdateDocumentStatusRequest(other.Id.Value, DocumentParseStatus.Completed)));

        Assert.Equal(DocumentParseStatus.Failed, failed.ParseStatus);
        Assert.Equal("Parser failed", failed.FailureReason);
        Assert.Equal(updatedAt, failed.UpdatedAt);
        Assert.Equal("Document was not found.", exception.Message);
    }

    private sealed class FixedCurrentProfileProvider : ICurrentProfileProvider
    {
        private readonly LocalProfileId profileId;

        public FixedCurrentProfileProvider(LocalProfileId profileId)
        {
            this.profileId = profileId;
        }

        public ValueTask<LocalProfileId> GetCurrentProfileIdAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(profileId);
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeDocumentRepository : IDocumentMetadataRepository
    {
        public List<UploadedDocument> Documents { get; } = [];

        public bool ThrowOnAdd { get; init; }

        public UploadedDocument AddExisting(LocalProfileId profileId, string name, DateTimeOffset createdAt)
        {
            var document = UploadedDocument.Create(
                profileId,
                name,
                DocumentMediaTypes.PlainText,
                5,
                ValidHash,
                createdAt);
            Documents.Add(document);
            return document;
        }

        public Task AddDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd)
            {
                throw new InvalidOperationException("Persistence failed.");
            }

            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UploadedDocument>> ListDocumentsAsync(
            LocalProfileId profileId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<UploadedDocument>>(
                Documents.Where(document => document.ProfileId == profileId).ToArray());
        }

        public Task<UploadedDocument?> GetDocumentAsync(
            LocalProfileId profileId,
            DocumentId documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<UploadedDocument?>(
                Documents.SingleOrDefault(document => document.ProfileId == profileId && document.Id == documentId));
        }

        public Task UpdateDocumentAsync(UploadedDocument document, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDocumentTemporaryStorage : IDocumentTemporaryStorage
    {
        private readonly string hash;
        private readonly long byteLength;

        public FakeDocumentTemporaryStorage(string hash, long byteLength)
        {
            this.hash = hash;
            this.byteLength = byteLength;
        }

        public TemporaryDocumentFile? SavedFile { get; private set; }

        public TemporaryDocumentFile? DeletedFile { get; private set; }

        public Task<TemporaryDocumentFile> SaveAsync(Stream content, CancellationToken cancellationToken = default)
        {
            SavedFile = new TemporaryDocumentFile(Guid.NewGuid(), byteLength);
            return Task.FromResult(SavedFile);
        }

        public Task<string> ComputeSha256HashAsync(
            TemporaryDocumentFile temporaryFile,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(hash);
        }

        public Task DeleteAsync(TemporaryDocumentFile temporaryFile, CancellationToken cancellationToken = default)
        {
            DeletedFile = temporaryFile;
            return Task.CompletedTask;
        }
    }
}
