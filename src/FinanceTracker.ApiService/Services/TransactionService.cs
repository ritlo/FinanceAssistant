using FinanceTracker.ApiService.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceTracker.ApiService.Services
{
    public record MonthlySummaryItem(string Category, decimal TotalAmount);
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

        // Async method for logging transactions from LLM backend
        public async Task<bool> LogTransactionAsync(decimal amount, string category, string description, DateTime date, string userId)
        {
            try
            {
                // Find or create the category
                var cat = _context.Categories.FindOne(c => c.Name == category);
                if (cat == null)
                {
                    cat = new Category { Name = category, Type = "Expense" };
                    _context.Categories.Insert(cat);
                }

                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Amount = amount,
                    Category = cat,
                    Description = description,
                    Date = date,
                    Type = TransactionType.Expense // Default to Expense; can be extended
                };
                _logger.LogInformation("[LLM][DB] Logging transaction for user {UserId}: {Amount} {Category} {Description} {Date}", userId, amount, category, description, date);
                _context.Transactions.Insert(transaction);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LLM][DB] Failed to log transaction for user {UserId}", userId);
                return false;
            }
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

        public IEnumerable<MonthlySummaryItem> GetMonthlySummary(string userId, int? month = null, int? year = null)
        {
            _logger.LogInformation("Getting monthly summary for user {UserId}, month {Month}, year {Year}", userId, month, year);

            var now = DateTime.UtcNow;
            int m = month ?? now.Month;
            int y = year ?? now.Year;

            // Validate month and year
            if (m < 1 || m > 12 || y < 1 || y > 9999)
            {
                _logger.LogWarning("Invalid month/year for summary: month={Month}, year={Year}", m, y);
                return Enumerable.Empty<MonthlySummaryItem>();
            }

            DateTime firstDayOfMonth;
            try
            {
                firstDayOfMonth = new DateTime(y, m, 1);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, "Invalid date for summary: month={Month}, year={Year}", m, y);
                return Enumerable.Empty<MonthlySummaryItem>();
            }
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var transactions = _context.Transactions.Find(t =>
                t.UserId == userId &&
                t.Date >= firstDayOfMonth &&
                t.Date <= lastDayOfMonth &&
                t.Type == TransactionType.Expense);

            var summary = transactions
                .GroupBy(t => t.Category.Name)
                .Select(g => new MonthlySummaryItem(g.Key, g.Sum(t => t.Amount)))
                .OrderByDescending(s => s.TotalAmount)
                .ToList();

            return summary;
        }

        // Returns the most recent N transactions for a user, sorted by date descending

        public async Task<List<Transaction>> GetRecentTransactionsAsync(string userId, int count = 10)
        {
            _logger.LogInformation("Getting recent {Count} transactions for user {UserId}", count, userId);
            var transactions = _context.Transactions
                .Find(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(count)
                .ToList();
            await Task.CompletedTask; // For async signature compatibility
            return transactions;
        }
    }
}
