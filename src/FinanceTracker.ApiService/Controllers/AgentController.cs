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
        public async Task<IActionResult> ProcessRequest([FromBody] AgentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest("User ID cannot be empty.");
            }

            try
            {
                var result = await _agentService.ProcessUserRequestAsync(request.Prompt, request.UserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the agent request.");
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }

        [HttpPost("stream-process")]
        public async Task StreamProcessRequest([FromBody] AgentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Prompt cannot be empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("User ID cannot be empty.");
                return;
            }

            Response.ContentType = "text/event-stream";

            try
            {
                await foreach (var chunk in _agentService.StreamUserRequestAsync(request.Prompt, request.UserId))
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
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string userId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is not provided or empty.");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID cannot be empty.");
            }

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

    public class AgentRequest
    {
        public string? Prompt { get; set; }
        public string? UserId { get; set; }
    }
}
