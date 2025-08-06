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

        [HttpGet("summary/monthly")]
        public IActionResult GetMonthlySummary([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }
            var summary = _transactionService.GetMonthlySummary(userId);
            return Ok(summary);
        }
    }
}
