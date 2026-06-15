using System.Text.RegularExpressions;
using FinanceAssistant.Domain.Finance.Categories;
using FinanceAssistant.Domain.Identity;
using FinanceAssistant.Infrastructure.Persistence.Documents;

namespace FinanceAssistant.Infrastructure.Persistence;

public sealed partial class LiteDbSchemaInitializer
{
    public const int CurrentSchemaVersion = 1;

    private const string MetadataId = "financeassistant";

    private readonly FinanceAssistantDataOptions options;
    private readonly LiteDbConnectionFactory connectionFactory;

    public LiteDbSchemaInitializer(FinanceAssistantDataOptions options)
    {
        this.options = options;
        connectionFactory = new LiteDbConnectionFactory(options);
    }

    public LocalProfileId Initialize()
    {
        ValidateOptions();

        var fullPath = Path.GetFullPath(options.DatabasePath);
        var databaseExists = File.Exists(fullPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var database = connectionFactory.Open();
        var metadata = database
            .GetCollection<MetadataDocument>(LiteDbCollectionNames.Metadata, LiteDB.BsonAutoId.Int32)
            .FindById(MetadataId);

        if (metadata is null)
        {
            if (databaseExists)
            {
                throw new InvalidOperationException("Existing database is missing FinanceAssistant metadata.");
            }

            return InitializeNewDatabase(database);
        }

        if (metadata.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException("Unsupported FinanceAssistant database schema version.");
        }

        if (!string.Equals(metadata.Currency, options.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configured currency does not match database currency.");
        }

        var profile = database
            .GetCollection<LocalProfileDocument>(LiteDbCollectionNames.LocalProfiles, LiteDB.BsonAutoId.Guid)
            .FindAll()
            .SingleOrDefault();

        if (profile is null)
        {
            throw new InvalidOperationException("FinanceAssistant database is missing the local profile.");
        }

        return new LocalProfileId(profile.Id);
    }

    private LocalProfileId InitializeNewDatabase(LiteDB.LiteDatabase database)
    {
        var profileId = LocalProfileId.New();
        database.GetCollection<MetadataDocument>(LiteDbCollectionNames.Metadata, LiteDB.BsonAutoId.Int32).Insert(new MetadataDocument
        {
            Id = MetadataId,
            SchemaVersion = CurrentSchemaVersion,
            Currency = options.Currency,
        });
        database.GetCollection<LocalProfileDocument>(LiteDbCollectionNames.LocalProfiles, LiteDB.BsonAutoId.Guid).Insert(
            new LocalProfileDocument
            {
                Id = profileId.Value,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var categoryCollection = database.GetCollection<CategoryDocument>(
            LiteDbCollectionNames.Categories,
            LiteDB.BsonAutoId.Guid);
        foreach (var defaultCategory in CategoryDefaults.All)
        {
            categoryCollection.Insert(CategoryDocument.FromCategory(
                Category.Create(profileId, defaultCategory.Name, defaultCategory.TransactionType)));
        }

        return profileId;
    }

    private void ValidateOptions()
    {
        if (!CurrencyCodePattern().IsMatch(options.Currency))
        {
            throw new InvalidOperationException("FinanceAssistant currency must be an uppercase ISO 4217 code.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            throw new InvalidOperationException("FinanceAssistant database path is required.");
        }
    }

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyCodePattern();
}
