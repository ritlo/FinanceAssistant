using LiteDB;
using System.IO;

namespace FinanceTracker.ApiService.Data
{
    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabase Database { get; }

        public LiteDbContext(string databasePath)
        {
            // Ensure the directory exists for the database file
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            Database = new LiteDatabase(databasePath);
        }

        public ILiteCollection<Category> Categories => Database.GetCollection<Category>("categories");
        public ILiteCollection<Transaction> Transactions => Database.GetCollection<Transaction>("transactions");
    }

    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
        ILiteCollection<Category> Categories { get; }
        ILiteCollection<Transaction> Transactions { get; }
    }
}
