using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Belumi.Infrastructure.AI;

public sealed class OpenAiChatService : IOpenAiChatService
{
    private readonly OpenAiOptions _options;

    public OpenAiChatService(IOptions<OpenAiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ChatCompletion> CompleteChatAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API Key is not configured.");
        }

        var model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model;
        var client = new ChatClient(model, apiKey);

        return await client.CompleteChatAsync(messages, options, cancellationToken);
    }
}
