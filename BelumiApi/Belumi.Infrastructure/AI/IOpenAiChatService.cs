using OpenAI.Chat;

namespace Belumi.Infrastructure.AI;

public interface IOpenAiChatService
{
    Task<ChatCompletion> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken = default);
}
