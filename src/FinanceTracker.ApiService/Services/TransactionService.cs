using FinanceTracker.ApiService.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceTracker.ApiService.Services
{
    public class TransactionService
    {
        private readonly ILiteDbContext _context;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ILiteDbContext context, ILogger<TransactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<Transaction> GetTransactions(string userId)
        {
            _logger.LogInformation("Getting transactions for user {UserId}", userId);
            return _context.Transactions.Find(t => t.UserId == userId).ToList();
        }

        public Transaction GetTransactionById(string userId, Guid id)
        {
            _logger.LogInformation("Getting transaction {TransactionId} for user {UserId}", id, userId);
            return _context.Transactions.FindOne(t => t.Id == id && t.UserId == userId);
        }

        public void AddTransaction(Transaction transaction)
        {
            transaction.Id = Guid.NewGuid(); // Ensure a new ID for new transactions
            _logger.LogInformation("Adding transaction for user {UserId}", transaction.UserId);
            _context.Transactions.Insert(transaction);
        }

        public void UpdateTransaction(Transaction transaction)
        {
            _logger.LogInformation("Updating transaction {TransactionId} for user {UserId}", transaction.Id, transaction.UserId);
            _context.Transactions.Update(transaction);
        }

        public void DeleteTransaction(string userId, Guid id)
        {
            _logger.LogInformation("Deleting transaction {TransactionId} for user {UserId}", id, userId);
            _context.Transactions.DeleteMany(t => t.Id == id && t.UserId == userId);
        }

        public IEnumerable<Category> GetCategories()
        {
            _logger.LogInformation("Getting all categories");
            return _context.Categories.FindAll().ToList();
        }

        public void AddCategory(Category category)
        {
            _logger.LogInformation("Adding new category {CategoryName}", category.Name);
            _context.Categories.Insert(category);
        }

        // Method to seed initial categories if none exist
        public void SeedCategories()
        {
            if (_context.Categories.Count() == 0)
            {
                _logger.LogInformation("Seeding initial categories");
                var categories = new List<Category>
                {
                    new Category { Name = "Groceries", Type = "Expense" },
                    new Category { Name = "Rent", Type = "Expense" },
                    new Category { Name = "Salary", Type = "Income" },
                    new Category { Name = "Utilities", Type = "Expense" },
                    new Category { Name = "Transportation", Type = "Expense" },
                    new Category { Name = "Entertainment", Type = "Expense" },
                    new Category { Name = "Dining Out", Type = "Expense" },
                    new Category { Name = "Investments", Type = "Income" },
                    new Category { Name = "Freelance", Type = "Income" }
                };
                _context.Categories.InsertBulk(categories);
            }
        }
    }
}
