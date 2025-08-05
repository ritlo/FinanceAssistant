using LiteDB;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.ApiService.Data
{
    public class Category
    {
        [BsonId]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // e.g., "Expense", "Income"
    }
}
