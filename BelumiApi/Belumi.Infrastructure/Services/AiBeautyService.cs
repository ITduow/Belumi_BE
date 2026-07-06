using System.Text.Json;
using System.Text.Json.Serialization;
using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.AI;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace Belumi.Infrastructure.Services;

public sealed class AiBeautyService(BelumiDbContext db, IOpenAiChatService openAiChatService) : IAiBeautyService
{
    // ────────────────────────────────────────────────────────────────────────
    // JSON Schema mô tả format trả về — GPT tự hiểu và fill vào
    // ────────────────────────────────────────────────────────────────────────
    private const string IngredientScanSchema = """
        {
          "type": "object",
          "required": ["safetyScore", "status", "summary", "beneficial", "neutral", "harmful", "recommendations"],
          "properties": {
            "safetyScore": {
              "type": "integer",
              "minimum": 0,
              "maximum": 100,
              "description": "Điểm an toàn tổng thể của công thức (0–100)"
            },
            "status": {
              "type": "string",
              "enum": ["safe", "warning", "danger"],
              "description": "safe = an toàn (>= 80), warning = cần chú ý (60–79), danger = có nguy cơ (< 60)"
            },
            "summary": {
              "type": "string",
              "description": "Tóm tắt đánh giá công thức bằng tiếng Việt, 1–2 câu"
            },
            "beneficial": {
              "type": "array",
              "description": "Các thành phần hoạt chất chính có lợi cho da",
              "items": { "$ref": "#/$defs/item" }
            },
            "neutral": {
              "type": "array",
              "description": "Các thành phần trung tính (dung môi, chất tạo kết cấu, bảo quản thông thường,...)",
              "items": { "$ref": "#/$defs/item" }
            },
            "harmful": {
              "type": "array",
              "description": "Các thành phần cần chú ý hoặc có nguy cơ kích ứng",
              "items": { "$ref": "#/$defs/item" }
            },
            "recommendations": {
              "type": "array",
              "description": "2–4 lời khuyên thực tế bằng tiếng Việt dựa trên công thức",
              "items": { "type": "string" },
              "minItems": 2,
              "maxItems": 4
            }
          },
          "$defs": {
            "item": {
              "type": "object",
              "required": ["name", "category", "safety", "reason"],
              "properties": {
                "name": {
                  "type": "string",
                  "description": "Tên thành phần INCI (giữ nguyên tiếng Anh/Latin)"
                },
                "category": {
                  "type": "string",
                  "description": "Nhóm chức năng bằng tiếng Việt, ví dụ: Hoạt chất, Chất giữ ẩm, Dung môi, Chất nhũ hóa, Chất bảo quản, Chất tạo kết cấu, Chất chống oxy hóa, Chất tẩy rửa, Hương liệu, Chất điều chỉnh pH..."
                },
                "safety": {
                  "type": "string",
                  "enum": ["safe", "neutral", "warning"],
                  "description": "safe = an toàn, neutral = trung tính, warning = cần chú ý"
                },
                "reason": {
                  "type": "string",
                  "description": "Giải thích ngắn gọn công dụng hoặc cảnh báo bằng tiếng Việt, 1 câu"
                }
              }
            }
          }
        }
        """;

    private const string SystemPrompt = """
        Bạn là chuyên gia phân tích thành phần mỹ phẩm.
        Nhiệm vụ: phân tích danh sách thành phần INCI và trả về JSON theo đúng schema được cung cấp.
        Yêu cầu:
        - Toàn bộ nội dung text (summary, category, reason, recommendations) phải bằng tiếng Việt.
        - Tên thành phần (name) giữ nguyên tiếng Anh/Latin theo chuẩn INCI.
        - Phải phân loại và giải thích TOÀN BỘ các thành phần có trong danh sách vào các nhóm tương ứng (beneficial, neutral, harmful). Không được bỏ sót thành phần nào.
        - Trả về JSON thuần túy, không thêm markdown hay giải thích ngoài JSON.
        """;

    // ────────────────────────────────────────────────────────────────────────
    // Public methods
    // ────────────────────────────────────────────────────────────────────────

    public IngredientLookupResult LookupIngredients(IngredientLookupRequest request) =>
        new("Phân tích thành phần hoàn tất.", [], [], []);

    public async Task<IngredientScanResult> AnalyzeIngredientLabel(IngredientScanRequest request)
    {
        try
        {
            return await AnalyzeWithGptAsync(request);
        }
        catch
        {
            return EmptyResult();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // GPT call
    // ────────────────────────────────────────────────────────────────────────

    private async Task<IngredientScanResult> AnalyzeWithGptAsync(IngredientScanRequest request)
    {
        var skinTypeNote = string.IsNullOrWhiteSpace(request.SkinType)
            ? string.Empty
            : $"\nLoại da: {request.SkinType}.";

        var allergyNote = request.Allergies is { Count: > 0 }
            ? $"\nDị ứng cần lưu ý: {string.Join(", ", request.Allergies)}."
            : string.Empty;

        var userMessage = $"""
            Phân tích danh sách INCI sau:{skinTypeNote}{allergyNote}

            {request.RawTextOrImageUrl}

            Trả về JSON theo schema:
            {IngredientScanSchema}
            """;

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(userMessage)
        };

        var completion = await openAiChatService.CompleteChatAsync(
            messages,
            new ChatCompletionOptions { MaxOutputTokenCount = 4000 });

        var rawJson = completion.Content[0].Text.Trim();

        // Bỏ markdown code block nếu có
        if (rawJson.StartsWith("```"))
        {
            var firstNewline = rawJson.IndexOf('\n');
            var lastFence = rawJson.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                rawJson = rawJson[(firstNewline + 1)..lastFence].Trim();
        }

        var gptResult = JsonSerializer.Deserialize<GptIngredientScanResult>(
            rawJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (gptResult is null) return EmptyResult();

        return new IngredientScanResult(
            gptResult.SafetyScore,
            gptResult.Status ?? "safe",
            gptResult.Summary ?? string.Empty,
            gptResult.Beneficial?.Select(x => new IngredientScanItem(x.Name, x.Category, x.Safety, x.Reason)).ToList() ?? [],
            gptResult.Neutral?.Select(x => new IngredientScanItem(x.Name, x.Category, x.Safety, x.Reason)).ToList() ?? [],
            gptResult.Harmful?.Select(x => new IngredientScanItem(x.Name, x.Category, x.Safety, x.Reason)).ToList() ?? [],
            gptResult.Recommendations ?? []);
    }

    private static IngredientScanResult EmptyResult() =>
        new(0, "safe", string.Empty, [], [], [], []);

    // ────────────────────────────────────────────────────────────────────────
    // Makeup (giữ nguyên)
    // ────────────────────────────────────────────────────────────────────────

    public MakeupConsultationResult ConsultMakeup(MakeupConsultationRequest request)
    {
        var isEvening = request.Occasion.Contains("party", StringComparison.OrdinalIgnoreCase)
            || request.Occasion.Contains("evening", StringComparison.OrdinalIgnoreCase);

        return new MakeupConsultationResult(
            isEvening ? "Soft Glam Glow" : "Clean Daily Radiance",
            request.SkinTone.Contains("warm", StringComparison.OrdinalIgnoreCase) ? "Cushion beige ấm, finish satin" : "Nền trung tính nhẹ, finish tự nhiên",
            isEvening ? "Mắt nâu ánh nhũ với liner đậm" : "Mắt taupe mờ với mascara cong",
            isEvening ? "Môi rose berry" : "Môi peachy nude balm",
            ["Skin Veil Cushion", "Soft Focus Blush", "Cloud Tint Lip"]);
    }

    public MakeupTryOnResult TryOnMakeup(MakeupTryOnRequest request)
    {
        var score = request.ProductType.Contains("lip", StringComparison.OrdinalIgnoreCase) ? 94 : 88;
        return new MakeupTryOnResult(
            request.ProductName,
            request.ProductType,
            request.Shade,
            request.HexColor,
            score,
            "Xem trước phía client sẽ overlay màu này lên vùng mặt được nhận diện.",
            ["Thoa theo từng lớp mỏng.", "Kiểm tra màu dưới ánh sáng tự nhiên.", "Lưu sản phẩm vào Wishlist để so sánh sau."]);
    }

    public async Task<IReadOnlyCollection<MakeupCatalogItem>> GetMakeupCatalogAsync(CancellationToken cancellationToken) =>
        await db.MakeupCatalogItems.AsNoTracking().OrderBy(x => x.ProductType).ToListAsync(cancellationToken);
}

// ────────────────────────────────────────────────────────────────────────────
// Internal DTOs — chỉ dùng để deserialize JSON từ GPT
// ────────────────────────────────────────────────────────────────────────────

file sealed class GptIngredientScanResult
{
    [JsonPropertyName("safetyScore")] public int SafetyScore { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("beneficial")] public List<GptScanItem>? Beneficial { get; set; }
    [JsonPropertyName("neutral")] public List<GptScanItem>? Neutral { get; set; }
    [JsonPropertyName("harmful")] public List<GptScanItem>? Harmful { get; set; }
    [JsonPropertyName("recommendations")] public List<string>? Recommendations { get; set; }
}

file sealed class GptScanItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; set; } = string.Empty;
    [JsonPropertyName("safety")] public string Safety { get; set; } = "neutral";
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
}
