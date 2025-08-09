
using FinanceTracker.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceTracker.ApiService.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public TransactionController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public IActionResult GetTransactions([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }
            var transactions = _transactionService.GetTransactions(userId);
            return Ok(transactions);
        }

        [HttpGet("summary/monthly")]
        public IActionResult GetMonthlySummary([FromQuery] string userId, [FromQuery] int? month, [FromQuery] int? year)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }
            var summary = _transactionService.GetMonthlySummary(userId, month, year);
            return Ok(summary);
        }
    }
}
