using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;

namespace InsightVault.Application.Features.Chat;

public interface IChatService
{
    Task<ChatResponseDto> AskAsync(
        AskQuestionQuery query,
        CancellationToken cancellationToken = default);
}
