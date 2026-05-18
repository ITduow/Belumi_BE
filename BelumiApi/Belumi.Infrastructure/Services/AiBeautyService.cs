using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

public sealed class AiBeautyService(BelumiDbContext db) : IAiBeautyService
{
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
