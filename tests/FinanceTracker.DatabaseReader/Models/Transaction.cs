using System;

namespace FinanceTracker.DatabaseReader.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Category Category { get; set; } = new Category();
        public string UserId { get; set; } = string.Empty;
    }

    public class Category
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
