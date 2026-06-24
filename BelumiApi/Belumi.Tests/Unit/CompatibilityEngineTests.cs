using Belumi.Core;
using Belumi.Core.DTOs;
using Belumi.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Belumi.Tests;

/// <summary>
/// Unit tests for the Compatibility Engine — core business logic.
/// Tests the Rule Engine, normalization pipeline, and personalized assessment.
/// </summary>
public sealed class CompatibilityEngineTests
{
    // Pass null for DbContext since pure methods don't hit DB
    private readonly CompatibilityEngine _engine = new(null!, NullLogger<CompatibilityEngine>.Instance);

    // ─────────────────────────────────────────────────────────────────
    // Helper: Create standard skin profiles
    // ─────────────────────────────────────────────────────────────────

    private static NormalizedSkinProfile OilyAcne() => new(
        SkinType: SkinTypes.Oily,
        Concerns: [SkinConcerns.Acne, SkinConcerns.EnlargedPores],
        Sensitivity: SensitivityLevels.Low,
        LastAnalyzedAt: DateTime.UtcNow.AddDays(-10),
        IsStale: false
    );

    private static NormalizedSkinProfile DrySensitive() => new(
        SkinType: SkinTypes.Dry,
        Concerns: [SkinConcerns.Dehydration],
        Sensitivity: SensitivityLevels.High,
        LastAnalyzedAt: DateTime.UtcNow.AddDays(-5),
        IsStale: false
    );

    private static NormalizedSkinProfile SensitiveRedness() => new(
        SkinType: SkinTypes.Sensitive,
        Concerns: [SkinConcerns.Redness],
        Sensitivity: SensitivityLevels.High,
        LastAnalyzedAt: DateTime.UtcNow.AddDays(-30),
        IsStale: false
    );

    private static NormalizedSkinProfile NormalSkin() => new(
        SkinType: SkinTypes.Normal,
        Concerns: [],
        Sensitivity: SensitivityLevels.Low,
        LastAnalyzedAt: DateTime.UtcNow.AddDays(-1),
        IsStale: false
    );

    // ─────────────────────────────────────────────────────────────────
    // EvaluateSingle: Beneficial cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Niacinamide_OilySkin_IsBeneficial()
    {
        var result = _engine.EvaluateSingle("Niacinamide", OilyAcne());
        Assert.Equal("beneficial", result.Status);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public void Panthenol_DrySkin_IsBeneficial()
    {
        var result = _engine.EvaluateSingle("Panthenol", DrySensitive());
        Assert.Equal("beneficial", result.Status);
    }

    [Fact]
    public void HyaluronicAcid_DrySkin_IsBeneficial()
    {
        var result = _engine.EvaluateSingle("Hyaluronic Acid", DrySensitive());
        Assert.Equal("beneficial", result.Status);
    }

    [Fact]
    public void CentellaAsiatica_SensitiveSkin_IsBeneficial()
    {
        var result = _engine.EvaluateSingle("Centella Asiatica", SensitiveRedness());
        Assert.Equal("beneficial", result.Status);
    }

    [Fact]
    public void Ceramide_DrySkin_IsBeneficial()
    {
        var result = _engine.EvaluateSingle("Ceramide", DrySensitive());
        Assert.Equal("beneficial", result.Status);
    }

    // ─────────────────────────────────────────────────────────────────
    // EvaluateSingle: Warning cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Fragrance_SensitiveSkin_IsWarning()
    {
        var result = _engine.EvaluateSingle("Fragrance", SensitiveRedness());
        Assert.Equal("warning", result.Status);
    }

    [Fact]
    public void AlcoholDenat_DrySensitive_IsWarning()
    {
        var result = _engine.EvaluateSingle("Alcohol Denat", DrySensitive());
        Assert.Equal("warning", result.Status);
    }

    [Fact]
    public void Retinol_SensitiveSkin_IsWarning()
    {
        var result = _engine.EvaluateSingle("Retinol", SensitiveRedness());
        Assert.Equal("warning", result.Status);
    }

    [Fact]
    public void BenzoylPeroxide_DrySensitive_IsWarning()
    {
        var result = _engine.EvaluateSingle("Benzoyl Peroxide", DrySensitive());
        Assert.Equal("warning", result.Status);
    }

    [Fact]
    public void CoconutOil_OilyAcne_IsWarning()
    {
        var result = _engine.EvaluateSingle("Coconut Oil", OilyAcne());
        Assert.Equal("warning", result.Status);
    }

    // ─────────────────────────────────────────────────────────────────
    // EvaluateSingle: Neutral cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Water_AnySkin_IsNeutral()
    {
        var result = _engine.EvaluateSingle("Water", OilyAcne());
        Assert.Equal("neutral", result.Status);
    }

    [Fact]
    public void UnknownIngredient_IsNeutral()
    {
        var result = _engine.EvaluateSingle("Dimethicone Crosspolymer", NormalSkin());
        Assert.Equal("neutral", result.Status);
    }

    // ─────────────────────────────────────────────────────────────────
    // Evaluate (Scan — multiple ingredients)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_MixedIngredients_GroupsCorrectly()
    {
        var ingredients = new[] { "Niacinamide", "Fragrance", "Water" };
        var result = _engine.Evaluate(ingredients, SensitiveRedness());

        // Niacinamide should NOT be beneficial for Sensitive+Redness (it's not in GoodForSkinType for Sensitive)
        // Fragrance should be harmful for Sensitive
        // Water should be neutral
        Assert.True(result.Harmful.Any(x => x.Name == "Fragrance"));
        Assert.True(result.Neutral.Any(x => x.Name == "Water"));
    }

    [Fact]
    public void Evaluate_AllBeneficial_HighScore()
    {
        var ingredients = new[] { "Niacinamide", "Salicylic Acid", "Azelaic Acid" };
        var result = _engine.Evaluate(ingredients, OilyAcne());

        Assert.True(result.Score >= 80);
        Assert.Equal("Rất phù hợp", result.Status);
        Assert.Equal(3, result.Beneficial.Count);
        Assert.Empty(result.Harmful);
    }

    [Fact]
    public void Evaluate_AllHarmful_LowScore()
    {
        var ingredients = new[] { "Fragrance", "Alcohol Denat", "Menthol" };
        var result = _engine.Evaluate(ingredients, SensitiveRedness());

        Assert.True(result.Score < 60);
        Assert.Equal(3, result.Harmful.Count);
        Assert.Empty(result.Beneficial);
    }

    // ─────────────────────────────────────────────────────────────────
    // Normalization: Alias Exact Match
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Alias_VitaminB3_MatchesNiacinamide()
    {
        var result = _engine.EvaluateSingle("Vitamin B3", OilyAcne());
        Assert.Equal("beneficial", result.Status);
    }

    [Fact]
    public void Alias_SodiumAscorbylPhosphate_MatchesVitaminC()
    {
        var result = _engine.EvaluateSingle("Sodium Ascorbyl Phosphate", OilyAcne());
        // Vitamin C is good for Oily + DarkSpots
        Assert.NotEqual("neutral", result.Status); // Should match a rule
    }

    // ─────────────────────────────────────────────────────────────────
    // Normalization: Contains Match (OCR-realistic names)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ContainsMatch_CentellaAsiaticaLeafExtract()
    {
        var result = _engine.EvaluateSingle("Centella Asiatica Leaf Extract", SensitiveRedness());
        Assert.Equal("beneficial", result.Status); // Should match centella asiatica
    }

    [Fact]
    public void ContainsMatch_SodiumHyaluronatesCrosspolymer()
    {
        var result = _engine.EvaluateSingle("Sodium Hyaluronate Crosspolymer", DrySensitive());
        Assert.Equal("beneficial", result.Status); // Should match hyaluronic acid
    }

    [Fact]
    public void ContainsMatch_AscorbylGlucosideSolution()
    {
        var result = _engine.EvaluateSingle("Ascorbyl Glucoside Solution", NormalSkin());
        // Should match vitamin c via alias
        Assert.NotEqual("neutral", result.Status);
    }

    // ─────────────────────────────────────────────────────────────────
    // GetGeneralInfo: goodFor / avoidFor
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void GetGeneralInfo_Niacinamide_HasGoodFor()
    {
        var info = _engine.GetGeneralInfo("Niacinamide");
        Assert.NotNull(info);
        Assert.NotEmpty(info.GoodFor);
        Assert.Empty(info.AvoidFor);
    }

    [Fact]
    public void GetGeneralInfo_Fragrance_HasAvoidFor()
    {
        var info = _engine.GetGeneralInfo("Fragrance");
        Assert.NotNull(info);
        Assert.NotEmpty(info.AvoidFor);
    }

    [Fact]
    public void GetGeneralInfo_Water_IsNeutral()
    {
        var info = _engine.GetGeneralInfo("Water");
        Assert.NotNull(info);
        Assert.Empty(info.GoodFor);
        Assert.Empty(info.AvoidFor);
    }

    [Fact]
    public void GetGeneralInfo_UnknownIngredient_ReturnsNull()
    {
        var info = _engine.GetGeneralInfo("Some Random Chemical XYZ");
        Assert.Null(info);
    }
}
