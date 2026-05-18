using Belumi.Core.Entities;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;

namespace Belumi.Infrastructure.Services;

public sealed class SkinAnalysisService(BelumiDbContext db) : ISkinAnalysisService
{
    public async Task<SkinAnalysis> AnalyzeAsync(Guid userId, string imageUrl, CancellationToken cancellationToken)
    {
        var samples = new[]
        {
            ("Combination", "Oiliness around T-zone, mild dullness", "Use gentle cleanser, niacinamide serum, light gel moisturizer, and SPF 50.", 82),
            ("Dry", "Dehydration lines, weak moisture barrier", "Add ceramide cream, reduce exfoliation, and use hydrating toner morning and night.", 76),
            ("Oily", "Visible shine, congestion risk", "Choose salicylic cleanser 2-3 times weekly, oil-free moisturizer, and clay mask weekly.", 79)
        };
        var picked = samples[Math.Abs(imageUrl.GetHashCode()) % samples.Length];
        var analysis = new SkinAnalysis
        {
            UserId = userId,
            ImageUrl = imageUrl,
            SkinType = picked.Item1,
            Concerns = picked.Item2,
            Recommendations = picked.Item3,
            Score = picked.Item4
        };

        db.SkinAnalyses.Add(analysis);
        await db.SaveChangesAsync(cancellationToken);
        return analysis;
    }
}
