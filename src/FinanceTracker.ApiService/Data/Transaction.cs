using LiteDB;
using System;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.ApiService.Data
{
    public class Transaction
    {
        [BsonId]
        public Guid Id { get; set; } // Using Guid for unique IDs in NoSQL

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; } // Income or Expense

        [BsonRef("categories")] // Reference to the Category collection
        public Category Category { get; set; } = new Category(); // Initialize with a default Category

        public string UserId { get; set; } = string.Empty; // To link transactions to users
    }

    public enum TransactionType
    {
        Income,
        Expense
    }
}
