using LiteDB;
using FinanceTracker.DatabaseReader.Models;
using System.Collections.Generic;
using System.Linq;

namespace FinanceTracker.DatabaseReader.Services
{
    public class LiteDbTransactionReader
    {
        private readonly string _dbPath;
        public LiteDbTransactionReader(string dbPath)
        {
            _dbPath = dbPath;
        }

        public List<Transaction> GetAllTransactions()
        {
            using var db = new LiteDatabase(_dbPath);
            var col = db.GetCollection<Transaction>("transactions");
            return col.FindAll().ToList();
        }
    }
}
