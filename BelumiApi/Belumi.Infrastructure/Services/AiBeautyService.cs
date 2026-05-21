using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class AiBeautyService(BelumiDbContext db) : IAiBeautyService
{
    private static readonly string[] HelpfulIngredients = ["Niacinamide", "Hyaluronic Acid", "Glycerin", "Ceramide", "Panthenol", "Centella", "Vitamin C"];
    private static readonly string[] WatchIngredients = ["Alcohol Denat", "Fragrance", "Parfum", "Menthol", "Essential Oil"];

    public IngredientLookupResult LookupIngredients(IngredientLookupRequest request)
    {
        var text = request.TextOrImageUrl.ToLowerInvariant();
        var watchlist = new List<string>();
        if (text.Contains("alcohol denat")) watchlist.Add("Alcohol denat: can be drying for sensitive skin.");
        if (text.Contains("fragrance") || text.Contains("parfum")) watchlist.Add("Fragrance/parfum: patch test if your skin is reactive.");
        if (text.Contains("salicylic")) watchlist.Add("Salicylic acid: avoid over-layering with strong exfoliants.");

        var safe = new[] { "Niacinamide", "Hyaluronic Acid", "Glycerin", "Ceramide" }
            .Where(x => text.Contains(x.ToLowerInvariant().Split(' ')[0]))
            .DefaultIfEmpty("Glycerin")
            .ToArray();

        return new IngredientLookupResult(
            "Mock OCR/Gemini fallback analysis completed. Connect Gemini and OCR scanner here for production.",
            safe,
            watchlist,
            ["Patch test first.", "Use sunscreen when using active ingredients.", "Keep routine simple for 2 weeks."]);
    }

    public IngredientScanResult AnalyzeIngredientLabel(IngredientScanRequest request)
    {
        var source = request.RawTextOrImageUrl ?? string.Empty;
        var lower = source.ToLowerInvariant();
        var beneficial = HelpfulIngredients
            .Where(x => lower.Contains(x.ToLowerInvariant().Split(' ')[0]))
            .Select(x => new IngredientScanItem(x, "Active", "safe", $"{x} supports hydration, tone or skin barrier."))
            .ToList();

        if (beneficial.Count == 0)
        {
            beneficial.Add(new IngredientScanItem("Glycerin", "Humectant", "safe", "Common moisturizing ingredient with strong tolerance."));
        }

        var harmful = WatchIngredients
            .Where(x => lower.Contains(x.ToLowerInvariant()))
            .Select(x => new IngredientScanItem(x, "Irritation risk", "warning", $"{x} may irritate reactive or dry skin. Patch test first."))
            .ToList();

        var neutral = new List<IngredientScanItem>
        {
            new("Aqua", "Solvent", "neutral", "Base solvent used in most cosmetic formulas."),
            new("Emollients", "Texture", "neutral", "Helps product spread and feel smoother.")
        };

        var score = Math.Clamp(92 - harmful.Count * 18 + beneficial.Count * 2, 35, 98);
        var status = harmful.Count == 0 ? "safe" : harmful.Count <= 2 ? "warning" : "danger";
        var summary = status == "safe"
            ? "Formula looks clean with useful moisturizing or barrier-supporting ingredients."
            : "Formula includes useful ingredients but has potential irritants for sensitive skin.";

        return new IngredientScanResult(
            score,
            status,
            summary,
            beneficial,
            neutral,
            harmful,
            ["Review the full INCI list before purchase.", "Patch test for 24 hours.", "Use SPF when using exfoliating or brightening actives."]);
    }

    public MakeupConsultationResult ConsultMakeup(MakeupConsultationRequest request)
    {
        var isEvening = request.Occasion.Contains("party", StringComparison.OrdinalIgnoreCase)
            || request.Occasion.Contains("evening", StringComparison.OrdinalIgnoreCase);

        return new MakeupConsultationResult(
            isEvening ? "Soft Glam Glow" : "Clean Daily Radiance",
            request.SkinTone.Contains("warm", StringComparison.OrdinalIgnoreCase) ? "Warm beige cushion, satin finish" : "Neutral light base, natural finish",
            isEvening ? "Brown shimmer lid with lifted liner" : "Taupe wash with curled lashes",
            isEvening ? "Rose berry tint" : "Peach nude balm",
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
            "Client-side preview should overlay this shade on the detected face region. Backend returns shade metadata and tips for the mobile app.",
            ["Apply in thin layers.", "Check shade in natural light.", "Save matched products to Wishlist for later comparison."]);
    }

    public async Task<IReadOnlyCollection<MakeupCatalogItem>> GetMakeupCatalogAsync(CancellationToken cancellationToken) =>
        await db.MakeupCatalogItems.AsNoTracking().OrderBy(x => x.ProductType).ToListAsync(cancellationToken);

    public async Task<string> BuildSkinConsultationContextAsync(Guid userId)
    {
        // 1. Thu thập User Context
        var user = await db.Users.Include(u => u.BeautyProfile)
                                 .FirstOrDefaultAsync(u => u.Id == userId);
                                 
        if (user == null || user.BeautyProfile == null)
        {
            return "Vui lòng cập nhật hồ sơ làm đẹp (Beauty Profile) để sử dụng tính năng này.";
        }

        // 2. Thu thập Domain Context (Mock cho đến khi Task 16 hoàn thành)
        // Khi làm xong Task 16, chúng ta có thể lấy danh sách này từ bảng Ingredients:
        // var unsafeIngredients = await db.Ingredients.Where(i => i.SafetyRating == "Caution").Select(i => i.Name).ToListAsync();
        var unsafeIngredients = new List<string> { "Salicylic Acid", "Retinol", "Benzoyl Peroxide", "Kojic Acid" };

        var cautionRule = string.Empty;
        if ((user.BeautyProfile.SkinType ?? "").Contains("Sensitive", StringComparison.OrdinalIgnoreCase))
        {
            cautionRule = $"- KHÔNG BAO GIỜ khuyên dùng các thành phần sau vì khách hàng có da yếu: {string.Join(", ", unsafeIngredients)}.";
        }

        // 3. Xây dựng Context Prompt (Context Injection)
        var contextPrompt = $@"
Bạn là bác sĩ da liễu độc quyền của hệ thống Belumi.
THÔNG TIN KHÁCH HÀNG:
- Tên khách hàng: {user.FullName}
- Loại da: {user.BeautyProfile.SkinType}
- Vấn đề da: {user.BeautyProfile.SkinConcerns}
- Dị ứng: {user.BeautyProfile.Allergies}

LUẬT AN TOÀN (BẮT BUỘC TUÂN THỦ):
{cautionRule}
- Nếu khách hàng có dị ứng, tuyệt đối tránh xa các thành phần gây dị ứng đó.
- Luôn trả về kết quả dưới định dạng chuyên nghiệp, dễ hiểu.

Dựa vào ngữ cảnh trên, hãy đưa ra lộ trình chăm sóc da cho khách hàng này.
";

        return contextPrompt.Trim();
    }
}
