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
        public IActionResult GetMonthlySummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "testUser123";
            var summary = _transactionService.GetMonthlySummary(userId);
            return Ok(summary);
        }
    }
}
