using InsightVault.Api.Auth;
using InsightVault.Application.Features.Chat;
using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsightVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponseDto>> Ask(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await chatService.AskAsync(
                new AskQuestionQuery(request.Question, User.GetRequiredUserId(), request.MaxSources ?? 5),
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public sealed record ChatRequest(
        string Question,
        int? MaxSources);
}
