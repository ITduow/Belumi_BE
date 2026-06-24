using Belumi.Core;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

/// <summary>
/// Rule-based Compatibility Engine.
/// Matches ingredient lists against a user's Skin Profile to produce
/// a Compatibility Score and personalized ingredient grouping.
/// </summary>
public sealed class CompatibilityEngine(BelumiDbContext db)
{
    // ─────────────────────────────────────────────────────────────────
    // BE-1: Get Latest Skin Profile
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the most recent SkinAnalysis for a user and normalizes it
    /// into a standard NormalizedSkinProfile.
    /// Returns null if the user has never completed a skin analysis.
    /// </summary>
    public async Task<NormalizedSkinProfile?> GetSkinProfileAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return null;

        var latest = await db.SkinAnalyses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AnalyzedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return null;

        return new NormalizedSkinProfile(
            SkinType: SkinTypes.Normalize(latest.SkinType),
            Concerns: SkinConcerns.Parse(latest.Concerns),
            Sensitivity: SensitivityLevels.Normalize(latest.SensitivityLevel)
        );
    }

    // ─────────────────────────────────────────────────────────────────
    // BE-3: Compatibility Calculator (for Scan — multiple ingredients)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a list of ingredient names against the user's skin profile.
    /// </summary>
    public CompatibilityResult Evaluate(IEnumerable<string> ingredientNames, NormalizedSkinProfile profile)
    {
        var beneficial = new List<CompatibilityItem>();
        var harmful = new List<CompatibilityItem>();
        var neutral = new List<CompatibilityItem>();

        foreach (var rawName in ingredientNames)
        {
            var name = rawName.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var key = NormalizeIngredientName(name);
            if (!Rules.TryGetValue(key, out var rule))
            {
                neutral.Add(new CompatibilityItem(name, "Thành phần phụ trợ thông thường.", "Không ảnh hưởng đáng kể đến da của bạn."));
                continue;
            }

            var assessment = AssessSingle(rule, profile);
            var item = new CompatibilityItem(name, rule.GeneralReason, assessment.Reasons.FirstOrDefault() ?? "");

            switch (assessment.Status)
            {
                case "beneficial":
                    beneficial.Add(item);
                    break;
                case "warning":
                    harmful.Add(item);
                    break;
                default:
                    neutral.Add(item);
                    break;
            }
        }

        var total = beneficial.Count + harmful.Count + neutral.Count;
        var score = total == 0 ? 50 : CalculateScore(beneficial.Count, harmful.Count, total);

        var status = score switch
        {
            >= 80 => "Rất phù hợp",
            >= 60 => "Phù hợp",
            >= 40 => "Cần lưu ý",
            _ => "Không khuyến nghị"
        };

        return new CompatibilityResult(score, status, beneficial, harmful, neutral);
    }

    // ─────────────────────────────────────────────────────────────────
    // BE-5: Personalized Assessment (for Search — single ingredient)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a single ingredient against the user's skin profile.
    /// Returns a PersonalizedAssessment with status and human-readable reasons.
    /// </summary>
    public PersonalizedAssessment EvaluateSingle(string ingredientName, NormalizedSkinProfile profile)
    {
        var key = NormalizeIngredientName(ingredientName);
        if (!Rules.TryGetValue(key, out var rule))
        {
            return new PersonalizedAssessment("neutral", ["Thành phần này không có tác động đặc biệt đến tình trạng da hiện tại của bạn."]);
        }

        return AssessSingle(rule, profile);
    }

    // ─────────────────────────────────────────────────────────────────
    // Internal: Assess one ingredient against profile
    // ─────────────────────────────────────────────────────────────────

    private static PersonalizedAssessment AssessSingle(IngredientRule rule, NormalizedSkinProfile profile)
    {
        var benefitReasons = new List<string>();
        var harmReasons = new List<string>();

        // Check GoodForSkinType
        if (rule.GoodForSkinType.Contains(profile.SkinType))
        {
            benefitReasons.Add($"Phù hợp với da {DisplaySkinType(profile.SkinType)} của bạn.");
        }

        // Check GoodForConcern
        foreach (var concern in profile.Concerns)
        {
            if (rule.GoodForConcern.Contains(concern))
            {
                benefitReasons.Add($"Hỗ trợ cải thiện tình trạng {DisplayConcern(concern)} của bạn.");
            }
        }

        // Check AvoidForSkinType
        if (rule.AvoidForSkinType.Contains(profile.SkinType))
        {
            harmReasons.Add($"Có thể không phù hợp với da {DisplaySkinType(profile.SkinType)} của bạn.");
        }

        // Check AvoidForConcern
        foreach (var concern in profile.Concerns)
        {
            if (rule.AvoidForConcern.Contains(concern))
            {
                harmReasons.Add($"Có thể làm tệ hơn tình trạng {DisplayConcern(concern)} của bạn.");
            }
        }

        // Check Sensitivity
        if (rule.AvoidIfSensitive && profile.Sensitivity is SensitivityLevels.High or SensitivityLevels.Medium)
        {
            harmReasons.Add("Cần lưu ý vì da bạn đang ở mức nhạy cảm.");
        }

        // Decide status: harmful wins if any harm reason exists
        if (harmReasons.Count > 0 && benefitReasons.Count == 0)
            return new PersonalizedAssessment("warning", harmReasons);

        if (harmReasons.Count > 0 && benefitReasons.Count > 0)
        {
            var combined = benefitReasons.Concat(harmReasons).ToList();
            return new PersonalizedAssessment("warning", combined);
        }

        if (benefitReasons.Count > 0)
            return new PersonalizedAssessment("beneficial", benefitReasons);

        return new PersonalizedAssessment("neutral", ["Không ảnh hưởng đáng kể đến da của bạn."]);
    }

    // ─────────────────────────────────────────────────────────────────
    // Score Calculation
    // ─────────────────────────────────────────────────────────────────

    private static int CalculateScore(int beneficialCount, int harmfulCount, int total)
    {
        // Base score starts at 70
        // Each beneficial ingredient adds points, each harmful subtracts
        var baseScore = 70.0;
        var benefitBonus = beneficialCount * (20.0 / Math.Max(total, 1));
        var harmPenalty = harmfulCount * (35.0 / Math.Max(total, 1));

        var score = baseScore + benefitBonus - harmPenalty;
        return (int)Math.Clamp(Math.Round(score), 10, 100);
    }

    // ─────────────────────────────────────────────────────────────────
    // Display helpers
    // ─────────────────────────────────────────────────────────────────

    private static string DisplaySkinType(string type) => type switch
    {
        SkinTypes.Oily => "dầu",
        SkinTypes.Dry => "khô",
        SkinTypes.Combination => "hỗn hợp",
        SkinTypes.Sensitive => "nhạy cảm",
        SkinTypes.Normal => "thường",
        _ => type
    };

    private static string DisplayConcern(string concern) => concern switch
    {
        SkinConcerns.Acne => "mụn",
        SkinConcerns.DarkSpots => "thâm nám",
        SkinConcerns.Redness => "đỏ da",
        SkinConcerns.EnlargedPores => "lỗ chân lông to",
        SkinConcerns.Dehydration => "thiếu nước",
        _ => concern
    };

    private static string NormalizeIngredientName(string name)
    {
        var key = name.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
        // Resolve alias to canonical rule key
        return Aliases.TryGetValue(key, out var canonical) ? canonical : key;
    }

    /// <summary>
    /// Maps INCI / alternative names to canonical rule keys.
    /// Covers cases where OCR output differs from common names.
    /// </summary>
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Vitamin C variants
        ["sodium ascorbyl phosphate"] = "vitamin c",
        ["ascorbyl glucoside"] = "vitamin c",
        ["ethyl ascorbic acid"] = "vitamin c",
        ["ascorbyl tetraisopalmitate"] = "vitamin c",
        ["magnesium ascorbyl phosphate"] = "vitamin c",
        ["3 o ethyl ascorbic acid"] = "vitamin c",

        // Hyaluronic Acid variants
        ["sodium hyaluronate"] = "hyaluronic acid",
        ["hydrolyzed hyaluronic acid"] = "hyaluronic acid",
        ["ha"] = "hyaluronic acid",

        // BHA
        ["bha"] = "salicylic acid",
        ["beta hydroxy acid"] = "salicylic acid",

        // AHA
        ["aha"] = "glycolic acid",
        ["alpha hydroxy acid"] = "glycolic acid",

        // PHA
        ["pha"] = "lactic acid",
        ["gluconolactone"] = "lactic acid",

        // Centella variants
        ["cica"] = "centella asiatica",
        ["centella"] = "centella asiatica",
        ["madecassoside"] = "centella asiatica",
        ["asiaticoside"] = "centella asiatica",

        // Retinoid variants
        ["vitamin a"] = "retinol",
        ["retinaldehyde"] = "retinal",
        ["retinyl palmitate"] = "retinol",

        // Panthenol variants
        ["vitamin b5"] = "panthenol",
        ["dexpanthenol"] = "panthenol",
        ["d panthenol"] = "panthenol",

        // Niacinamide variants
        ["vitamin b3"] = "niacinamide",
        ["nicotinamide"] = "niacinamide",

        // Ceramide variants
        ["ceramide ap"] = "ceramide",
        ["ceramide eop"] = "ceramide",
        ["ceramide ng"] = "ceramide",
        ["ceramide ns"] = "ceramide",

        // Fragrance variants
        ["aroma"] = "fragrance",

        // SLS variants
        ["sodium laureth sulfate"] = "sodium lauryl sulfate",

        // Vitamin E
        ["vitamin e"] = "tocopherol",
        ["tocopheryl acetate"] = "tocopherol",

        // Tea Tree
        ["melaleuca alternifolia leaf oil"] = "tea tree oil",
        ["tea tree"] = "tea tree oil",

        // Arbutin
        ["arbutin"] = "alpha arbutin",
        ["beta arbutin"] = "alpha arbutin",
    };

    // ─────────────────────────────────────────────────────────────────
    // BE-2: Rule Mapping (Ingredient ↔ SkinType / Concern)
    // ─────────────────────────────────────────────────────────────────

    private sealed record IngredientRule(
        string GeneralReason,
        string[] GoodForSkinType,
        string[] GoodForConcern,
        string[] AvoidForSkinType,
        string[] AvoidForConcern,
        bool AvoidIfSensitive = false
    );

    private static readonly Dictionary<string, IngredientRule> Rules = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Actives: Acne & Oil Control ──
        ["niacinamide"] = new(
            "Kiểm soát dầu, giảm thâm, hỗ trợ hàng rào bảo vệ da.",
            GoodForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.Acne, SkinConcerns.DarkSpots, SkinConcerns.EnlargedPores],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["salicylic acid"] = new(
            "BHA giúp thông thoáng lỗ chân lông, giảm mụn đầu đen và mụn ẩn.",
            GoodForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.Acne, SkinConcerns.EnlargedPores],
            AvoidForSkinType: [SkinTypes.Dry],
            AvoidForConcern: [],
            AvoidIfSensitive: true
        ),
        ["benzoyl peroxide"] = new(
            "Hoạt chất diệt khuẩn gây mụn, giảm viêm.",
            GoodForSkinType: [SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.Acne],
            AvoidForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["azelaic acid"] = new(
            "Giảm viêm, giảm thâm sau mụn, làm đều màu da.",
            GoodForSkinType: [SkinTypes.Oily, SkinTypes.Combination, SkinTypes.Sensitive],
            GoodForConcern: [SkinConcerns.Acne, SkinConcerns.DarkSpots, SkinConcerns.Redness],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["tea tree oil"] = new(
            "Kháng khuẩn tự nhiên hỗ trợ giảm mụn.",
            GoodForSkinType: [SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.Acne],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [],
            AvoidIfSensitive: true
        ),

        // ── Brightening & Dark Spots ──
        ["vitamin c"] = new(
            "Chống oxy hóa, làm sáng da, giảm thâm.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.DarkSpots],
            AvoidForSkinType: [],
            AvoidForConcern: [],
            AvoidIfSensitive: true
        ),
        ["ascorbic acid"] = new(
            "Dạng Vitamin C nguyên chất, làm sáng da và chống oxy hóa mạnh.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.DarkSpots],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [],
            AvoidIfSensitive: true
        ),
        ["alpha arbutin"] = new(
            "Làm sáng da, giảm thâm nám nhẹ nhàng.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Oily, SkinTypes.Sensitive],
            GoodForConcern: [SkinConcerns.DarkSpots],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["kojic acid"] = new(
            "Ức chế sản xuất melanin, làm sáng da.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.DarkSpots],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [],
            AvoidIfSensitive: true
        ),
        ["tranexamic acid"] = new(
            "Giảm thâm nám, ức chế sản sinh melanin. Phổ biến trong skincare Việt.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Oily, SkinTypes.Sensitive],
            GoodForConcern: [SkinConcerns.DarkSpots, SkinConcerns.Redness],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),

        // ── Hydration & Barrier Repair ──
        ["hyaluronic acid"] = new(
            "Cấp nước sâu, giữ ẩm cho da.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Sensitive, SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["glycerin"] = new(
            "Chất giữ ẩm phổ biến, an toàn cho mọi loại da.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Normal, SkinTypes.Combination, SkinTypes.Sensitive, SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["ceramide"] = new(
            "Củng cố hàng rào bảo vệ da, giảm mất nước.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive, SkinTypes.Normal],
            GoodForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["ceramide np"] = new(
            "Củng cố hàng rào bảo vệ da, giảm mất nước.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive, SkinTypes.Normal],
            GoodForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["squalane"] = new(
            "Dưỡng ẩm nhẹ, không bít tắc lỗ chân lông.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Normal, SkinTypes.Sensitive],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["panthenol"] = new(
            "Vitamin B5, phục hồi da và làm dịu kích ứng.",
            GoodForSkinType: [SkinTypes.Sensitive, SkinTypes.Dry, SkinTypes.Normal],
            GoodForConcern: [SkinConcerns.Redness, SkinConcerns.Dehydration],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["centella asiatica"] = new(
            "Rau má, làm dịu da và hỗ trợ phục hồi.",
            GoodForSkinType: [SkinTypes.Sensitive, SkinTypes.Normal, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.Redness, SkinConcerns.Acne],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["allantoin"] = new(
            "Làm dịu da, hỗ trợ tái tạo tế bào.",
            GoodForSkinType: [SkinTypes.Sensitive, SkinTypes.Dry, SkinTypes.Normal],
            GoodForConcern: [SkinConcerns.Redness],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),
        ["shea butter"] = new(
            "Bơ hạt mỡ, dưỡng ẩm sâu cho da khô.",
            GoodForSkinType: [SkinTypes.Dry],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [SkinTypes.Oily],
            AvoidForConcern: [SkinConcerns.Acne]
        ),

        // ── Potential Irritants / Drying ──
        ["alcohol denat"] = new(
            "Cồn khô, bay hơi nhanh, có thể làm khô da.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["sd alcohol"] = new(
            "Cồn khô, bay hơi nhanh, có thể làm khô da.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["fragrance"] = new(
            "Hương liệu, có nguy cơ gây kích ứng.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["parfum"] = new(
            "Hương liệu, có nguy cơ gây kích ứng.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["menthol"] = new(
            "Tạo cảm giác mát nhưng dễ gây kích ứng.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Sensitive, SkinTypes.Dry],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["essential oil"] = new(
            "Tinh dầu có thể gây kích ứng cho da nhạy cảm.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["sodium lauryl sulfate"] = new(
            "Chất tẩy rửa mạnh, có thể phá vỡ hàng rào bảo vệ da.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["sls"] = new(
            "Chất tẩy rửa mạnh, có thể phá vỡ hàng rào bảo vệ da.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Dry, SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Dehydration, SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),

        // ── Comedogenic (pore-clogging) ──
        ["coconut oil"] = new(
            "Dầu dừa, dưỡng ẩm tốt nhưng dễ bít tắc lỗ chân lông.",
            GoodForSkinType: [SkinTypes.Dry],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            AvoidForConcern: [SkinConcerns.Acne, SkinConcerns.EnlargedPores]
        ),
        ["cocoa butter"] = new(
            "Bơ ca cao, dưỡng ẩm nhưng có thể gây bít tắc.",
            GoodForSkinType: [SkinTypes.Dry],
            GoodForConcern: [SkinConcerns.Dehydration],
            AvoidForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            AvoidForConcern: [SkinConcerns.Acne, SkinConcerns.EnlargedPores]
        ),
        ["isopropyl myristate"] = new(
            "Chất làm mềm da, có tính gây bít tắc lỗ chân lông cao.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            AvoidForConcern: [SkinConcerns.Acne, SkinConcerns.EnlargedPores]
        ),
        ["isopropyl palmitate"] = new(
            "Chất làm mềm da, có tính gây bít tắc lỗ chân lông cao.",
            GoodForSkinType: [],
            GoodForConcern: [],
            AvoidForSkinType: [SkinTypes.Oily, SkinTypes.Combination],
            AvoidForConcern: [SkinConcerns.Acne, SkinConcerns.EnlargedPores]
        ),

        // ── Retinoids (professional caution) ──
        ["retinol"] = new(
            "Vitamin A, chống lão hóa và giảm mụn. Cần thận trọng khi bắt đầu.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Oily, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.Acne, SkinConcerns.DarkSpots],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["retinal"] = new(
            "Dạng Vitamin A mạnh hơn retinol, hiệu quả nhanh hơn.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Oily],
            GoodForConcern: [SkinConcerns.Acne, SkinConcerns.DarkSpots],
            AvoidForSkinType: [SkinTypes.Sensitive, SkinTypes.Dry],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),

        // ── Exfoliating Acids ──
        ["glycolic acid"] = new(
            "AHA tẩy tế bào chết, làm sáng da.",
            GoodForSkinType: [SkinTypes.Normal, SkinTypes.Oily, SkinTypes.Combination],
            GoodForConcern: [SkinConcerns.DarkSpots, SkinConcerns.EnlargedPores],
            AvoidForSkinType: [SkinTypes.Sensitive],
            AvoidForConcern: [SkinConcerns.Redness],
            AvoidIfSensitive: true
        ),
        ["lactic acid"] = new(
            "AHA nhẹ, tẩy tế bào chết và giữ ẩm.",
            GoodForSkinType: [SkinTypes.Dry, SkinTypes.Normal, SkinTypes.Sensitive],
            GoodForConcern: [SkinConcerns.DarkSpots, SkinConcerns.Dehydration],
            AvoidForSkinType: [],
            AvoidForConcern: []
        ),

        // ── Common Neutral Ingredients ──
        ["aqua"] = new("Nước, dung môi nền của hầu hết sản phẩm.", [], [], [], []),
        ["water"] = new("Nước, dung môi nền của hầu hết sản phẩm.", [], [], [], []),
        ["butylene glycol"] = new("Dung môi giữ ẩm nhẹ, an toàn.", [], [], [], []),
        ["phenoxyethanol"] = new("Chất bảo quản phổ biến, an toàn ở nồng độ cho phép.", [], [], [], []),
        ["carbomer"] = new("Chất tạo đặc, tạo kết cấu gel.", [], [], [], []),
        ["xanthan gum"] = new("Chất tạo đặc tự nhiên.", [], [], [], []),
        ["disodium edta"] = new("Chất ổn định công thức sản phẩm.", [], [], [], []),
        ["tocopherol"] = new("Vitamin E, chống oxy hóa nhẹ.", [], [], [], []),
    };
}
