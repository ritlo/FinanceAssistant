
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FinanceTracker.ApiService.Data;
using FinanceTracker.ApiService.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FinanceTracker.ApiService.Services
{
    public record MonthlySummaryItem(string Category, decimal TotalAmount);
    public class TransactionService
    {
        // Heuristic-based categorization for required categories
        private static readonly Dictionary<string, string[]> CategoryKeywords = new()
        {
            { "Food and Drinks", new[] { "restaurant", "food", "meal", "dine", "cafe", "breakfast", "lunch", "dinner", "pizza", "burger", "snack", "eatery", "bar", "pub", "drink", "coffee", "tea", "juice", "wine", "beer", "cocktail", "brew" } },
            { "Groceries", new[] { "grocer", "supermarket", "grocery", "market", "store", "mart" } },
            { "Shopping", new[] { "shop", "store", "mall", "retail", "clothes", "apparel", "fashion", "electronics", "purchase", "buy" } },
            { "Travel", new[] { "flight", "airline", "hotel", "taxi", "uber", "lyft", "bus", "train", "travel", "trip", "journey", "booking", "expedia", "airbnb" } },
            { "Services", new[] { "service", "repair", "clean", "maintenance", "subscription", "consult", "fee", "support", "utility", "internet", "phone", "cell", "insurance" } },
            { "Entertainment", new[] { "movie", "cinema", "theater", "concert", "music", "game", "netflix", "spotify", "show", "event", "ticket", "amusement", "park" } },
            { "Health", new[] { "pharmacy", "doctor", "hospital", "clinic", "health", "medicine", "drug", "dentist", "optician", "fitness", "gym", "workout", "yoga" } },
            { "Transport", new[] { "transport", "bus", "train", "taxi", "uber", "lyft", "metro", "subway", "cab", "ride", "commute", "fare" } }
        };

        private string AutoCategorize(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "Services"; // fallback
            var desc = description.ToLowerInvariant();
            foreach (var kvp in CategoryKeywords)
            {
                foreach (var keyword in kvp.Value)
                {
                    if (desc.Contains(keyword))
                        return kvp.Key;
                }
            }
            return "Services"; // fallback if no match
        }

        private readonly ILiteDbContext _context;
        private readonly ILogger<TransactionService> _logger;
        private readonly IHubContext<TransactionHub>? _hubContext;

        public TransactionService(ILiteDbContext context, ILogger<TransactionService> logger, IHubContext<TransactionHub>? hubContext = null)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public IEnumerable<Transaction> GetTransactions(string userId)
        {
            _logger.LogInformation("Getting transactions for user {UserId}", userId);
            return _context.Transactions.Include(x => x.Category).Find(t => t.UserId == userId).ToList();
        }

        public Transaction GetTransactionById(string userId, Guid id)
        {
            _logger.LogInformation("Getting transaction {TransactionId} for user {UserId}", id, userId);
            return _context.Transactions.Include(x => x.Category).FindOne(t => t.Id == id && t.UserId == userId);
        }

        public void AddTransaction(Transaction transaction)
        {
            transaction.Id = Guid.NewGuid(); // Ensure a new ID for new transactions
            // Auto-categorize if category is null or empty or not in required set
            var requiredCategories = CategoryKeywords.Keys.ToList();
            string categoryName = transaction.Category != null && transaction.Category.Name != null ? transaction.Category.Name : string.Empty;
            if (string.IsNullOrWhiteSpace(categoryName) || !requiredCategories.Contains(categoryName))
            {
                categoryName = AutoCategorize(transaction.Description);
            }
            // Find or create the category
            var cat = _context.Categories.FindOne(c => c.Name == categoryName);
            if (cat == null)
            {
                cat = new Category { Name = categoryName, Type = "Expense" };
                var newId = _context.Categories.Insert(cat);
                _logger.LogInformation("Created new category: {CategoryName} with ID {CategoryId}", cat.Name, newId);
                // Reload the category from the database to ensure correct reference
                cat = _context.Categories.FindOne(c => c.Name == categoryName);
            }
            else
            {
                _logger.LogInformation("Using existing category: {CategoryName} with ID {CategoryId}", cat.Name, cat.Id);
            }
            transaction.Category = cat;
            _logger.LogInformation("Adding transaction for user {UserId}", transaction.UserId);
            _context.Transactions.Insert(transaction);
            // Notify clients
            _hubContext?.Clients.All.SendAsync("TransactionChanged");
        }

        // Async method for logging transactions from LLM backend
        public async Task<bool> LogTransactionAsync(decimal amount, string category, string description, DateTime date, string userId)
        {
            try
            {
                // Use auto-categorization if category is null/empty or not in required set
                var requiredCategories = CategoryKeywords.Keys.ToList();
                string categoryName = string.IsNullOrWhiteSpace(category) || !requiredCategories.Contains(category) ? AutoCategorize(description) : category;
                // Find or create the category
                var cat = _context.Categories.FindOne(c => c.Name == categoryName);
                if (cat == null)
                {
                    cat = new Category { Name = categoryName, Type = "Expense" };
                    var newId = _context.Categories.Insert(cat);
                    _logger.LogInformation("[LLM][DB] Created new category: {CategoryName} with ID {CategoryId}", cat.Name, newId);
                    // Reload the category from the database to ensure correct reference
                    cat = _context.Categories.FindOne(c => c.Name == categoryName);
                }
                else
                {
                    _logger.LogInformation("[LLM][DB] Using existing category: {CategoryName} with ID {CategoryId}", cat.Name, cat.Id);
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
                _logger.LogInformation("[LLM][DB] Logging transaction for user {UserId}: {Amount} {Category} {Description} {Date}", userId, amount, categoryName, description, date);
                _context.Transactions.Insert(transaction);
                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("TransactionChanged");
                }
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
            _hubContext?.Clients.All.SendAsync("TransactionChanged");
        }

        public void DeleteTransaction(string userId, Guid id)
        {
            _logger.LogInformation("Deleting transaction {TransactionId} for user {UserId}", id, userId);
            _context.Transactions.DeleteMany(t => t.Id == id && t.UserId == userId);
            _hubContext?.Clients.All.SendAsync("TransactionChanged");
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
                    // Required 8 categories (Expense)
                    new Category { Name = "Food and Drinks", Type = "Expense" },
                    new Category { Name = "Groceries", Type = "Expense" },
                    new Category { Name = "Shopping", Type = "Expense" },
                    new Category { Name = "Travel", Type = "Expense" },
                    new Category { Name = "Services", Type = "Expense" },
                    new Category { Name = "Entertainment", Type = "Expense" },
                    new Category { Name = "Health", Type = "Expense" },
                    new Category { Name = "Transport", Type = "Expense" },
                    // Keep Rent (Expense)
                    new Category { Name = "Rent", Type = "Expense" },
                    // Keep Income types
                    new Category { Name = "Salary", Type = "Income" },
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

            var transactions = _context.Transactions
                .Include(x => x.Category)
                .Find(t =>
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
                .Include(x => x.Category)
                .Find(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(count)
                .ToList();
            await Task.CompletedTask; // For async signature compatibility
            return transactions;
        }
    }
}
