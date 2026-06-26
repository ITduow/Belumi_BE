using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Belumi.Core.Interfaces;
using Belumi.Core.DTOs.Gemini;
using Belumi.Infrastructure.AI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Belumi.Infrastructure.Services;

public class SkinAnalysisService : ISkinAnalysisService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SkinAnalysisService> _logger;
    private readonly IOpenAiChatService _openAiChatService;

    public SkinAnalysisService(
        IMemoryCache cache,
        ILogger<SkinAnalysisService> logger,
        IOpenAiChatService openAiChatService)
    {
        _cache             = cache;
        _logger            = logger;
        _openAiChatService = openAiChatService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<AnalysisResponse> AnalyzeAsync(byte[] imageBytes, string skinType)
    {
        var validationError = ValidateImage(imageBytes);
        if (validationError != null)
        {
            _logger.LogWarning("Image validation failed: {Error}", validationError);
            return new AnalysisResponse { Status = "retake_required", Message = validationError };
        }

        byte[] processedImage;
        try
        {
            processedImage = PreprocessImage(imageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image preprocessing failed");
            return new AnalysisResponse
            {
                Status  = "error",
                Message = $"Ảnh không hợp lệ hoặc bị lỗi: {ex.Message}"
            };
        }

        var imageHash = ComputeHash(processedImage);
        var cacheKey  = $"skin:{skinType}:{imageHash}";

        if (_cache.TryGetValue(cacheKey, out SkinAnalysisResult? cached) && cached != null)
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return new AnalysisResponse { Status = "success", Result = cached, FromCache = true };
        }

        SkinAnalysisResult? result;
        try
        {
            result = await CallOpenAiAsync(processedImage, skinType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed");
            return new AnalysisResponse { Status = "error", Message = $"Lỗi AI: {ex.Message}" };
        }

        if (result == null)
        {
            _logger.LogWarning("OpenAI returned null result for skinType={SkinType}", skinType);
            return new AnalysisResponse { Status = "error", Message = "AI không trả về kết quả hợp lệ" };
        }

        if (!result.FaceDetected || result.ImageSubject != "face")
        {
            _logger.LogWarning("Rejected non-face image. faceDetected={FaceDetected}, subject={ImageSubject}",
                result.FaceDetected, result.ImageSubject);
            return new AnalysisResponse
            {
                Status  = "retake_required",
                Message = "Ảnh không chứa khuôn mặt rõ để phân tích da. Vui lòng chọn ảnh selfie hoặc ảnh khuôn mặt rõ hơn."
            };
        }

        _logger.LogInformation("OpenAI confidence={Confidence}, acne={Acne}",
            result.Confidence, result.AcneLevel);

        if (result.Confidence < 0.65)
        {
            return new AnalysisResponse
            {
                Status  = "retake_required",
                Message = "Ảnh chưa đủ rõ. Vui lòng chụp ở nơi có ánh sáng tốt hơn."
            };
        }

        result.SkinCondition = GenerateSkinCondition(result);
        result.Description   = GenerateDescription(result, skinType);

        result.RecommendedIngredients   = GenerateRecommendedIngredients(result, skinType);
        result.AvoidOrProfessionalOnly  = GenerateAvoidOrProfessionalOnly(result, skinType);

        _cache.Set(cacheKey, result, TimeSpan.FromHours(24));

        return new AnalysisResponse { Status = "success", Result = result, FromCache = false };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SKIN CONDITION
    // ─────────────────────────────────────────────────────────────────────────

    private static string GenerateSkinCondition(SkinAnalysisResult result)
    {
        var concernCount = 0;
        if (result.AcneLevel != "none") concernCount++;
        if (result.PigmentationLevel is "medium" or "high") concernCount++;
        if (result.PoreVisibilityLevel is "medium" or "high") concernCount++;
        if (result.VisibleRednessLevel is "medium" or "high") concernCount++;

        return result.AcneLevel switch
        {
            "severe"   => "critical",
            "moderate" => "needs_care",
            "mild"     => "needs_attention",
            "none"     => concernCount >= 2 ? "needs_attention" : "good",
            _          => "good"
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DESCRIPTION — 2 câu ngắn gọn, tự nhiên
    // ─────────────────────────────────────────────────────────────────────────

    private static string GenerateDescription(SkinAnalysisResult result, string skinType)
    {
        var concerns = new List<string>();
        if (result.AcneLevel != "none") concerns.Add(result.AcneLevel switch
        {
            "mild"     => "mụn nhẹ",
            "moderate" => "mụn mức trung bình",
            "severe"   => "mụn nặng",
            _          => "mụn"
        });
        if (result.PigmentationLevel is "medium" or "high") concerns.Add("thâm");
        if (result.PoreVisibilityLevel is "medium" or "high") concerns.Add("lỗ chân lông to");
        if (result.VisibleRednessLevel is "medium" or "high") concerns.Add("vùng đỏ");

        string sentence1;
        if (!concerns.Any())
        {
            sentence1 = "Da bạn đang trong tình trạng ổn, hãy cố gắng duy trì routine chăm sóc da hiện tại.";
        }
        else
        {
            var concernText = concerns.Count == 1
                ? concerns[0]
                : string.Join(", ", concerns[..^1]) + " và " + concerns[^1];

            sentence1 = $"Da bạn có {concernText}.";
        }

        var sentence2 = (result.AcneLevel, skinType, result.PigmentationLevel is "medium" or "high") switch
        {
            ("severe", _, _) =>
                "Tình trạng khá nghiêm trọng, nên tham khảo bác sĩ da liễu thay vì tự điều trị.",

            ("moderate", "sensitive", _) =>
                "Da nhạy cảm với mụn trung bình dễ để lại sẹo — cần ưu tiên làm dịu trước khi dùng active.",
            ("moderate", _, true) =>
                "Mụn trung bình kèm thâm cần được xử lý sớm để tránh thâm ăn sâu hơn.",
            ("moderate", _, _) =>
                "Tình trạng cần được chăm sóc tích cực hơn, nên bắt đầu routine ổn định sớm.",

            ("mild", _, true) =>
                "Tình trạng chưa nghiêm trọng nhưng cần xử lý sớm để tránh thâm kéo dài.",
            ("mild", "oily", _) =>
                "Mụn nhẹ trên da dầu thường cải thiện tốt nếu kiểm soát được bã nhờn và giữ routine ổn định.",
            ("mild", "dry", _) =>
                "Mụn nhẹ trên da khô thường do barrier yếu — ưu tiên dưỡng ẩm trước khi thêm treatment.",
            ("mild", _, _) =>
                "Tình trạng hiện tại nhẹ, có thể cải thiện rõ với routine đơn giản và kiên trì.",

            ("none", "sensitive", _) when result.VisibleRednessLevel is "medium" or "high" =>
                "Đỏ da trên da nhạy cảm cần theo dõi — ưu tiên sản phẩm dịu nhẹ và phục hồi barrier.",
            ("none", "dry", _) when result.PigmentationLevel is "medium" or "high" =>
                "Da khô kèm thâm cần dưỡng ẩm tốt trước khi dùng active làm sáng để tránh kích ứng.",
            ("none", "oily", _) when result.PoreVisibilityLevel is "medium" or "high" =>
                "Lỗ chân lông to trên da dầu thường do bã nhờn tích tụ — BHA định kỳ sẽ giúp cải thiện.",
            ("none", _, _) when concerns.Any() =>
                "Các dấu hiệu hiện tại chưa nghiêm trọng, duy trì routine đều đặn sẽ cải thiện tốt.",

            _ => "Duy trì routine chăm sóc da sáng và tối là đủ để giữ da ở trạng thái tốt."
        };

        var acneTypeSentence = GenerateAcneTypeSentence(result);

        return string.IsNullOrWhiteSpace(acneTypeSentence)
            ? $"{sentence1} {sentence2}"
            : $"{sentence1} {acneTypeSentence} {sentence2}";
    }

    private static string GenerateAcneTypeSentence(SkinAnalysisResult result)
    {
        if (result.AcneLevel == "none" || result.AcneTypes.Count == 0)
            return string.Empty;

        var labels = result.AcneTypes
            .Select(type => type switch
            {
                "closed_comedone_like" => "mụn ẩn/mụn đầu trắng đóng",
                "open_comedone_like" => "mụn đầu đen",
                "papule_like" => "sẩn đỏ",
                "pustule_like" => "mụn mủ",
                "nodule_or_cyst_like" => "nốt viêm sâu dạng nang/cục",
                _ => string.Empty
            })
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (labels.Count == 0)
            return string.Empty;

        var acneTypeText = labels.Count == 1
            ? labels[0]
            : string.Join(", ", labels[..^1]) + " và " + labels[^1];

        return $"Các dạng mụn nhìn thấy gồm {acneTypeText}.";
    }

    private static List<IngredientRecommendation> GenerateRecommendedIngredients(SkinAnalysisResult result, string skinType)
    {
        var items = new Dictionary<string, IngredientRecommendation>(StringComparer.OrdinalIgnoreCase);
        var treatAsSensitive = ShouldTreatAsSensitive(result, skinType);

        if (treatAsSensitive)
        {
            AddIngredient(items, "ceramides", "Hỗ trợ dưỡng ẩm và củng cố hàng rào bảo vệ da khi da đang đỏ hoặc dễ kích ứng.", "aad_pick_moisturizer");
            AddIngredient(items, "hyaluronic acid", "Hỗ trợ cấp ẩm, phù hợp khi cần ưu tiên phục hồi và giảm khô căng.", "aad_pick_moisturizer");
            AddIngredient(items, "fragrance-free moisturizer", "Giảm nguy cơ kích ứng từ hương liệu trong giai đoạn da đang nhạy cảm.", "fda_fragrances");
            AddIngredient(items, "broad-spectrum SPF 30+ sunscreen", "Chống nắng hằng ngày giúp hạn chế thâm và bảo vệ vùng da đang viêm đỏ.", "aad_sunscreen_faq");
            return items.Values.OrderBy(x => x.Name).ToList();
        }

        if (result.AcneLevel != "none")
        {
            AddIngredient(items, "azelaic acid", "Có thể hỗ trợ da có mụn và thâm sau mụn trong routine phù hợp.", "aad_acne_treatment");
            AddIngredient(items, "benzoyl peroxide", "Hoạt chất trị mụn phổ biến, phù hợp hơn khi da không đang đỏ hoặc quá nhạy cảm.", "aad_acne_treatment");
            AddIngredient(items, "salicylic acid", "BHA hỗ trợ mụn và bít tắc lỗ chân lông khi dùng thận trọng.", "aad_acne_treatment");
        }

        if (result.PigmentationLevel is "medium" or "high")
        {
            AddIngredient(items, "azelaic acid", "Có thể hỗ trợ thâm và da không đều màu.", "aad_acne_treatment");
            AddIngredient(items, "niacinamide", "Có thể hỗ trợ hàng rào da và tình trạng không đều màu trong routine dịu nhẹ.", "aad_pick_moisturizer");
            AddIngredient(items, "vitamin C", "Có thể hỗ trợ làm sáng và đều màu da khi da dung nạp tốt.", "aad_pick_moisturizer");
        }

        if (skinType == "oily" || result.PoreVisibilityLevel is "medium" or "high")
        {
            AddIngredient(items, "salicylic acid", "BHA có thể hỗ trợ dầu thừa, bít tắc và lỗ chân lông rõ.", "aad_acne_treatment");
        }

        if (result.VisibleRednessLevel is "medium" or "high")
        {
            AddIngredient(items, "broad-spectrum SPF 30+ sunscreen", "Chống nắng giúp bảo vệ vùng da đỏ và giảm nguy cơ thâm sau viêm.", "aad_sunscreen_faq");
        }

        return items.Values.OrderBy(x => x.Name).ToList();
    }

    private static List<IngredientRecommendation> GenerateAvoidOrProfessionalOnly(SkinAnalysisResult result, string skinType)
    {
        var items = new Dictionary<string, IngredientRecommendation>(StringComparer.OrdinalIgnoreCase);
        var treatAsSensitive = ShouldTreatAsSensitive(result, skinType);

        if (treatAsSensitive)
        {
            AddIngredient(items, "retinoids", "Có thể gây khô, bong tróc hoặc kích ứng; nên hỏi chuyên gia khi da đang đỏ, nhạy cảm hoặc mụn nặng.", "aad_acne_treatment");
            AddIngredient(items, "glycolic acid / exfoliating acids", "Acid tẩy da chết có thể làm da đang đỏ hoặc yếu hàng rào bảo vệ kích ứng thêm.", "aad_isotretinoin_skin_care");
            AddIngredient(items, "salicylic acid", "Có thể hữu ích cho mụn nhưng nên thận trọng khi da đang đỏ hoặc nhạy cảm.", "aad_acne_treatment");
            AddIngredient(items, "benzoyl peroxide", "Có thể gây khô, rát hoặc kích ứng; không nên tự dùng mạnh khi da đang viêm đỏ.", "aad_acne_treatment");
            AddIngredient(items, "fragrance", "Hương liệu có thể gây nhạy cảm hoặc kích ứng ở một số người.", "fda_fragrances");
            AddIngredient(items, "physical scrub", "Chà xát cơ học có thể làm vùng viêm đỏ kích ứng thêm.", "aad_acne_treatment");
        }

        if (result.AcneLevel == "severe")
        {
            AddIngredient(items, "strong acne actives", "Mụn nặng nên được đánh giá bởi bác sĩ da liễu trước khi phối nhiều hoạt chất mạnh.", "aad_acne_treatment");
        }

        return items.Values.OrderBy(x => x.Name).ToList();
    }

    private static bool ShouldTreatAsSensitive(SkinAnalysisResult result, string skinType) =>
        skinType == "sensitive" || result.VisibleRednessLevel is "medium" or "high" || result.AcneLevel == "severe";

    private static void AddIngredient(
        Dictionary<string, IngredientRecommendation> items,
        string name,
        string reason,
        params string[] sourceIds)
    {
        if (items.ContainsKey(name)) return;
        items[name] = new IngredientRecommendation
        {
            Name = name,
            Reason = reason,
            SourceIds = sourceIds.ToList()
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IMAGE UTILS
    // ─────────────────────────────────────────────────────────────────────────

    private string? ValidateImage(byte[] imageBytes)
    {
        if (imageBytes.Length > 5 * 1024 * 1024)
            return "Ảnh quá lớn. Vui lòng chọn ảnh nhỏ hơn 5MB.";

        if (!IsJpegOrPng(imageBytes))
            return "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ JPEG và PNG.";

        return null;
    }

    private static bool IsJpegOrPng(byte[] bytes)
    {
        if (bytes.Length < 4) return false;

        bool isJpeg = bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        bool isPng  = bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;

        return isJpeg || isPng;
    }

    private static byte[] PreprocessImage(byte[] imageBytes)
    {
        using var input = new MemoryStream(imageBytes);
        using var image = Image.Load(input);

        image.Mutate(ctx => ctx
            .Resize(new ResizeOptions
            {
                Size     = new Size(512, 512),
                Mode     = ResizeMode.Pad,
                Position = AnchorPositionMode.Center
            })
            .Brightness(1.05f)
            .Contrast(1.05f)
        );

        using var output = new MemoryStream();
        image.Save(output, new JpegEncoder { Quality = 85 });
        return output.ToArray();
    }

    private static string ComputeHash(byte[] imageBytes)
    {
        var hash = SHA256.HashData(imageBytes);
        return Convert.ToHexString(hash).ToLower();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPENAI CHAT CLIENT
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<SkinAnalysisResult?> CallOpenAiAsync(byte[] imageBytes, string skinType)
    {
        var prompt = BuildPrompt(skinType);
        var messages = new ChatMessage[]
        {
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(prompt),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(imageBytes), "image/jpeg")
            )
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "skin_analysis",
                BinaryData.FromString(GetJsonSchemaForSkinAnalysisResult()),
                "Face skin analysis result schema",
                true
            ),
            Temperature = 0.1f
        };

        ChatCompletion completion = await _openAiChatService.CompleteChatAsync(messages, options);
        
        if (completion.Content == null || completion.Content.Count == 0)
        {
            _logger.LogWarning("OpenAI returned empty response");
            return null;
        }

        var resultText = completion.Content[0].Text;
        if (string.IsNullOrWhiteSpace(resultText))
        {
            _logger.LogWarning("OpenAI returned empty text in response");
            return null;
        }

        return JsonSerializer.Deserialize<SkinAnalysisResult>(resultText);
    }

    private static string GetJsonSchemaForSkinAnalysisResult() => """
    {
      "type": "object",
      "properties": {
        "face_detected": { "type": "boolean" },
        "image_subject": { "type": "string", "enum": ["face", "animal", "object", "landscape", "unknown"] },
        "acne_level": { "type": "string", "enum": ["none", "mild", "moderate", "severe"] },
        "acne_types": {
          "type": "array",
          "items": { "type": "string", "enum": ["closed_comedone_like", "open_comedone_like", "papule_like", "pustule_like", "nodule_or_cyst_like"] }
        },
        "oiliness_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "oiliness_zones": {
          "type": "array",
          "items": { "type": "string", "enum": ["forehead", "nose", "chin", "cheeks"] }
        },
        "pore_visibility_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "pigmentation_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "skin_tone_evenness_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "visible_redness_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "visible_wrinkle_level": { "type": "string", "enum": ["low", "medium", "high"] },
        "confidence": { "type": "number" }
      },
      "required": [
        "face_detected", "image_subject", "acne_level", "acne_types",
        "oiliness_level", "oiliness_zones", "pore_visibility_level",
        "pigmentation_level", "skin_tone_evenness_level", "visible_redness_level",
        "visible_wrinkle_level", "confidence"
      ],
      "additionalProperties": false
    }
    """;

    private static string BuildPrompt(string skinType) => $"""
        You are a professional skincare analysis AI.

        The user has already been identified as having {skinType} skin.
        Do NOT re-classify skin type — it is already known.

        First determine whether the image contains a visible human face suitable for facial skin analysis.
        If the image does not contain a human face, set face_detected=false and image_subject to the best matching category.
        If the image contains an animal, object, landscape, product photo, or other non-face subject, do not infer skin concerns.
        If face_detected=false, set acne_level="none", acne_types=[], all ordinal levels to "low", oiliness_zones=[], and confidence based only on subject detection.

        Your task: Analyze the facial image and identify VISIBLE skin concerns only when face_detected=true.
        For each visible feature, assign a severity level using only what is visually observable.
        Do not infer biological properties that cannot be seen directly.

        Use:
        low = minimal / barely visible
        medium = noticeable but localized
        high = obvious, widespread, or strong

        Acne severity:
        Classify acne_level using a Hayashi-inspired estimate of visible inflammatory lesions on one half of the face.
        Inflammatory lesions include papules and pustules.
        - none: 0 clearly visible inflammatory lesions and no notable non-inflammatory acne
        - mild: 1-5 visible inflammatory lesions, or clearly visible mild non-inflammatory acne such as closed comedones, whiteheads, or blackheads
        - moderate: 6-20 visible inflammatory lesions
        - severe: 21 or more visible inflammatory lesions, dense inflamed clusters, or nodular/cystic-looking acne

        Do not count dark spots, acne scars, pores, freckles, shadows, makeup texture, or image noise as acne lesions.
        If the image is unclear, lower confidence instead of guessing aggressively.

        Identify visible acne-like lesion types and return all that apply in acne_types:
        - closed_comedone_like: small smooth dome-shaped skin-colored, whitish, or grayish bumps, often called closed comedones or "mụn ẩn".
        - open_comedone_like: small dark gray, brown, or black follicular plugs, often called blackheads.
        - papule_like: small red or pink raised bumps without visible pus.
        - pustule_like: red inflamed bumps with a visible white or yellow pus-like center.
        - nodule_or_cyst_like: large, deep-looking, swollen red bumps or clustered severe inflammatory lesions. Do not diagnose cysts; only mark cyst-like or nodule-like appearance.
        If no acne-like lesions are visible, return acne_types=[].

        Other visible skin signs:
        
        oiliness_level:
        Estimate visible surface oil shine only.
        - low: matte skin, minimal specular highlights
        - medium: visible shine in 1–2 facial zones
        - high: strong reflective shine across T-zone or multiple zones
        
        oiliness_zones:
        Return facial zones where oil shine is concentrated (forehead, nose, chin, cheeks).
        
        pore_visibility_level:
        - low: pores barely noticeable
        - medium: visible pores in limited areas (nose/inner cheeks)
        - high: clearly enlarged pores across multiple areas
        
        pigmentation_level:
        - low: few isolated marks
        - medium: multiple visible spots or clustered PIH
        - high: widespread or dense hyperpigmentation
        
        skin_tone_evenness_level:
        - low: tone looks mostly uniform
        - medium: mild unevenness in brightness or pigmentation
        - high: obvious patchiness or multiple uneven-toned regions
        
        visible_redness_level:
        Evaluate only visible redness at capture time.
        - low: little to no redness
        - medium: localized redness
        - high: broad or intense redness
        
        visible_wrinkle_level:
        - low: no obvious visible lines
        - medium: visible fine lines in limited areas
        - high: multiple clear lines or deeper creases

        Do not mark concerns with higher levels when they are likely caused only by shadows, uneven lighting, flash, filters, makeup, blur, or low image quality.

        confidence: How confident you are in this analysis based on image quality (0.0 to 1.0).

        Respond ONLY with valid JSON matching the schema. No explanations, no markdown.
        """;
}
