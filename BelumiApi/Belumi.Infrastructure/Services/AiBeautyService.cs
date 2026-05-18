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
}
