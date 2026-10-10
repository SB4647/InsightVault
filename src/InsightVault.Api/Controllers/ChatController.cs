using InsightVault.Api.Auth;
using InsightVault.Application.Features.Chat;
using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InsightVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("interactive")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await chatService.AskAsync(
                new AskQuestionQuery(request.Question, User.GetRequiredUserId()),
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public sealed record ChatRequest(string Question);
}
