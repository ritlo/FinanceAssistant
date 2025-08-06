using FinanceTracker.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;

namespace FinanceTracker.ApiService.Controllers
{
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        private readonly AgentService _agentService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(AgentService agentService, ILogger<AgentController> logger)
        {
            _agentService = agentService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            // TODO: Replace with real authentication
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "testUser123";

            try
            {
                var result = await _agentService.ProcessUserRequestAsync(prompt, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the agent request.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("stream-process")]
        public async Task StreamProcessRequest([FromBody] string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Prompt cannot be empty.");
                return;
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "testUser123";
            Response.ContentType = "text/event-stream";

            try
            {
                await foreach (var chunk in _agentService.StreamUserRequestAsync(prompt, userId))
                {
                    await Response.WriteAsync($"data: {chunk}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while streaming the agent response.");
                await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is not provided or empty.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "testUser123";

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    await _agentService.ProcessDocumentStreamAsync(stream, userId, file.FileName);
                }
                return Ok("File processed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the uploaded document.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
