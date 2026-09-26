using MarketLink.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketLink.Controllers;

/// <summary>
/// Backing endpoint for the floating AI assistant widget. It is a POST that
/// takes a message and returns the assistant's reply as JSON.
/// </summary>
public class AiController : Controller
{
    private readonly IAiAssistantService _assistant;

    public AiController(IAiAssistantService assistant)
    {
        _assistant = assistant;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var reply = await _assistant.GetReplyAsync(request?.Message ?? string.Empty);
        return Json(new { reply });
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
