using FinanceTracker.ApiService.Data;
using FinanceTracker.ApiService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace FinanceTracker.ApiService.Models.Plugins
{
    public class FinancialPlugin
    {
        private readonly TransactionService _transactionService;
        private readonly ILogger<FinancialPlugin> _logger;

        public FinancialPlugin(TransactionService transactionService, ILogger<FinancialPlugin> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [KernelFunction, Description("Adds a new financial transaction (income or expense) to the user's record.")]
        public string AddTransaction(
            KernelArguments arguments,
            [Description("The amount of the transaction.")] decimal amount,
            [Description("The date of the transaction in YYYY-MM-DD format.")] string date,
            [Description("A brief description of the transaction.")] string description,
            [Description("The type of transaction, either 'Income' or 'Expense'.")] string type,
            [Description("The category of the transaction (e.g., 'Groceries', 'Rent', 'Salary').")] string categoryName)
        {
            var userId = arguments["userId"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID is not available in the context.");
                return "Error: User ID is not available in the context.";
            }

            _logger.LogInformation("Adding transaction for user {UserId}", userId);

            if (!Enum.TryParse(type, true, out TransactionType transactionType))
            {
                _logger.LogError("Invalid transaction type '{Type}'", type);
                return $"Error: Invalid transaction type '{type}'. Must be 'Income' or 'Expense'.";
            }

            if (!DateTime.TryParse(date, out DateTime transactionDate))
            {
                _logger.LogError("Invalid date format '{Date}'", date);
                return $"Error: Invalid date format '{date}'. Please use YYYY-MM-DD.";
            }

            var categories = _transactionService.GetCategories();
            var category = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) && c.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                _logger.LogError("Category '{CategoryName}' of type '{Type}' not found", categoryName, type);
                return $"Error: Category '{categoryName}' of type '{type}' not found. Please add it first or choose an existing one.";
            }

            var transaction = new Transaction
            {
                Amount = amount,
                Date = transactionDate,
                Description = description,
                Type = transactionType,
                Category = category,
                UserId = userId
            };

            _transactionService.AddTransaction(transaction);
            var successMessage = $"Transaction '{description}' of {amount:C} ({type}) in category '{categoryName}' on {date} added successfully.";
            _logger.LogInformation(successMessage);
            return successMessage;
        }

        [KernelFunction, Description("Retrieves a summary of financial spending or income for a user.")]
        public string GetSpendingSummary(
            KernelArguments arguments,
            [Description("The time period for the summary (e.g., 'this month', 'last quarter', '2023').")] string timePeriod,
            [Description("The type of transaction to summarize ('Income', 'Expense', or 'All').")] string transactionType,
            [Description("The specific category to filter by (optional).")] string? categoryName)
        {
            var userId = arguments["userId"]?.ToString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("User ID is not available in the context.");
                return "Error: User ID is not available in the context.";
            }

            _logger.LogInformation("Getting spending summary for user {UserId}", userId);

            var transactions = _transactionService.GetTransactions(userId).AsQueryable();

            if (Enum.TryParse(transactionType, true, out TransactionType typeFilter))
            {
                transactions = transactions.Where(t => t.Type == typeFilter);
            }
            else if (!transactionType.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Invalid transaction type filter '{TransactionType}'", transactionType);
                return $"Error: Invalid transaction type filter '{transactionType}'. Must be 'Income', 'Expense', or 'All'.";
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                transactions = transactions.Where(t => t.Category != null && t.Category.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            }

            // Basic time period parsing (can be expanded)
            var now = DateTime.UtcNow;
            DateTime startDate;
            DateTime endDate = now;

            switch (timePeriod.ToLowerInvariant())
            {
                case "this month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "last month":
                    startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
                case "this year":
                    startDate = new DateTime(now.Year, 1, 1);
                    break;
                case "last year":
                    startDate = new DateTime(now.Year - 1, 1, 1);
                    endDate = new DateTime(now.Year - 1, 12, 31);
                    break;
                default:
                    // Attempt to parse as a year
                    if (int.TryParse(timePeriod, out int year))
                    {
                        startDate = new DateTime(year, 1, 1);
                        endDate = new DateTime(year, 12, 31);
                    }
                    else
                    {
                        _logger.LogError("Unsupported time period '{TimePeriod}'", timePeriod);
                        return $"Error: Unsupported time period '{timePeriod}'. Try 'this month', 'last month', 'this year', 'last year', or a specific year (e.g., '2023').";
                    }
                    break;
            }

            transactions = transactions.Where(t => t.Date >= startDate && t.Date <= endDate);

            if (!transactions.Any())
            {
                var notFoundMessage = $"No {transactionType.ToLower()} transactions found for {timePeriod} {(string.IsNullOrWhiteSpace(categoryName) ? "" : $"in category '{categoryName}'")}.";
                _logger.LogInformation(notFoundMessage);
                return notFoundMessage;
            }

            var totalAmount = transactions.Sum(t => t.Amount);
            var summary = $"Total {transactionType.ToLower()} for {timePeriod} {(string.IsNullOrWhiteSpace(categoryName) ? "" : $"in category '{categoryName}'")}: {totalAmount:C}.";

            // Optionally, add more detailed breakdown
            if (string.IsNullOrWhiteSpace(categoryName) && transactions.Any())
            {
                var categoryBreakdown = transactions
                    .GroupBy(t => t.Category != null ? t.Category.Name : "Uncategorized")
                    .Select(g => $"{g.Key}: {g.Sum(t => t.Amount):C}")
                    .ToList();
                summary += "\nBreakdown by category:\n" + string.Join("\n", categoryBreakdown);
            }

            _logger.LogInformation("Returning summary: {Summary}", summary);
            return summary;
        }
    }
}
