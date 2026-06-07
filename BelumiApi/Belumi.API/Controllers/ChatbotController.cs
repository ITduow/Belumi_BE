using Belumi.Core.Entities;
using Belumi.Infrastructure.AI;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/chatbot")]
public sealed class ChatbotController(
    BelumiDbContext db,
    BelumiChatPlugin chatPlugin,
    ChatbotRequestContext requestContext,
    ChatToolCallTracker toolTracker,
    IOptions<OpenAiOptions> openAiOptions) : ControllerBase
{
    [HttpPost("message")]
    [AllowAnonymous]
    public async Task<ActionResult<ChatbotResponse>> Message(ChatbotRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        var message = request.Message.Trim();
        var userId = User.GetUserId();
        requestContext.UserId = userId;

        var answer = await ComposeAnswerAsync(message, request.SkinType, cancellationToken);

        if (userId != Guid.Empty)
        {
            db.AiUsageLogs.Add(new AiUsageLog
            {
                UserId = userId,
                FeatureName = "chatbot",
                TokenUsed = Math.Max(1, (message.Length + answer.Length) / 4),
                RequestData = message,
                ResponseData = answer
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var sources = toolTracker.Sources
            .Select(x => new ChatSource(x.Type, x.Label, x.Url))
            .ToArray();

        return Ok(new ChatbotResponse(answer, toolTracker.Tools, sources));
    }

    private async Task<string> ComposeAnswerAsync(string userMessage, string? skinType, CancellationToken cancellationToken)
    {
        var options = openAiOptions.Value;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API Key is not configured.");
        }

        var model = string.IsNullOrWhiteSpace(options.Model) ? "gpt-4o-mini" : options.Model;
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(model, apiKey);
        var kernel = kernelBuilder.Build();
        kernel.Plugins.AddFromObject(chatPlugin, "Belumi");

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.25,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var prompt = $$"""
            SYSTEM ROLE
            Bạn là Belumi AI Assistant, trợ lý AI của nền tảng Belumi. Nhiệm vụ của bạn là cung cấp thông tin chăm sóc da, thành phần mỹ phẩm, phân tích routine, phân tích INCI list, gợi ý phù hợp với loại da, và hỗ trợ người dùng hiểu dữ liệu trong hệ thống Belumi.

            DOMAIN SCOPE
            Chỉ trả lời các câu hỏi thuộc phạm vi:
            - Chăm sóc da, loại da, tình trạng da, routine sáng/tối.
            - Thành phần mỹ phẩm, INCI, công dụng, độ phù hợp với da mụn, da nhạy cảm, da dầu, da khô.
            - Cách kết hợp hoặc tránh kết hợp hoạt chất skincare.
            - Sản phẩm, ingredient, dữ liệu, lịch sử phân tích da, hoặc tính năng có trong nền tảng Belumi.
            - Hướng dẫn sử dụng app/web Belumi nếu câu hỏi liên quan trực tiếp đến nền tảng.

            OUT OF SCOPE
            Trước khi trả lời, hãy tự phân loại câu hỏi. Nếu câu hỏi không liên quan Belumi, skincare, mỹ phẩm, cosmetic ingredients, routine, sản phẩm skincare, hoặc dữ liệu trong nền tảng Belumi thì không gọi tool và không trả lời nội dung đó.
            Ví dụ ngoài phạm vi: giá khách sạn, vé máy bay, du lịch, lập trình, toán học, chính trị, tài chính, pháp luật, nấu ăn, game, phim, tin tức, hoặc yêu cầu đóng vai không đúng vai trò Belumi.
            Khi ngoài phạm vi, chỉ trả lời lịch sự và ngắn gọn:
            "Mình là trợ lý của Belumi nên chỉ có thể hỗ trợ các câu hỏi về chăm sóc da, thành phần mỹ phẩm và dữ liệu trong nền tảng Belumi. Bạn có thể hỏi mình về routine, loại da, ingredient hoặc sản phẩm skincare nhé."

            TOOL POLICY
            - Với câu hỏi về ingredient hoặc product trong database Belumi: ưu tiên dùng tool tìm dữ liệu local trước.
            - Với câu hỏi về ingredient mới, công dụng, độ phù hợp với da mụn, da nhạy cảm, da dầu, da khô, độ an toàn, bằng chứng, hiệu quả, hoặc cách kết hợp: dùng INCI API tools khi cần.
            - Với câu hỏi về lịch sử hoặc phân tích da cá nhân: dùng latest skin analysis tool, chỉ cho user hiện tại.
            - Không tự bịa inventory sản phẩm Belumi nếu tool/local data không có.
            - Không tự bịa endpoint, dữ liệu API, giá, stock, discount, hoặc thông tin chưa có trong tool context.

            ANSWER RULES
            - Trả lời bằng tiếng Việt, trừ khi user yêu cầu ngôn ngữ khác.
            - Câu trả lời phải thực tế, ngắn gọn, dễ hiểu.
            - Nếu thiếu dữ liệu, nói rõ là chưa có dữ liệu.
            - Không chẩn đoán bệnh da liễu như bác sĩ.
            - Với da nhạy cảm, dị ứng, đang mang thai, mụn nặng, kích ứng kéo dài: khuyên patch test và gặp bác sĩ da liễu khi cần.
            - Không khẳng định tuyệt đối kiểu "chắc chắn an toàn 100%" hoặc "chữa khỏi".
            - Không trả lời lan man ngoài câu hỏi.

            RESPONSE STYLE
            - Thân thiện, lịch sự, tự nhiên.
            - Có thể chia bullet ngắn nếu câu hỏi cần so sánh hoặc gợi ý routine.
            - Không dùng markdown quá dài nếu không cần.

            USER_DECLARED_SKIN_TYPE:
            {{skinType ?? "unknown"}}

            USER_MESSAGE:
            {{userMessage}}
            """;

        var result = await kernel.InvokePromptAsync(
            prompt,
            new KernelArguments(executionSettings),
            cancellationToken: cancellationToken);
        var answer = result.GetValue<string>() ?? result.ToString();

        return !string.IsNullOrWhiteSpace(answer)
            ? answer
            : "Mình chưa tạo được câu trả lời lúc này. Bạn thử hỏi lại ngắn hơn một chút nhé.";
    }
}

public sealed record ChatbotRequest(string Message, string? SkinType);

public sealed record ChatbotResponse(string Answer, IReadOnlyCollection<string> Tools, IReadOnlyCollection<ChatSource> Sources);

public sealed record ChatSource(string Type, string Label, string? Url);
