using System.Text.Json;
using Belumi.Core;
using Belumi.Core.DTOs;
using Belumi.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Belumi.Tests;

/// <summary>
/// Golden dataset regression tests for the Compatibility Engine.
/// Loads a fixed set of known-good classifications from JSON.
/// If any classification changes after rule/alias modification → test fails → build fails.
/// </summary>
public sealed class GoldenDatasetTests
{
    private readonly CompatibilityEngine _engine = new(null!, NullLogger<CompatibilityEngine>.Instance);
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public GoldenDatasetTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    private const double BaselineMatchRate = 57.7;

    private static readonly Lazy<List<GoldenProduct>> _products = new(() =>
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Regression", "TestData", "golden_dataset_v1.json");
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<GoldenDataset>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        return doc.Products;
    });

    private static List<GoldenProduct> Products => _products.Value;

    // ─────────────────────────────────────────────────────────────────
    // Regression: Beneficial ingredients must stay beneficial
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AllExpectedBeneficial_AreClassifiedBeneficial()
    {
        var failures = new List<string>();
        int verifiedCount = 0;

        foreach (var product in Products)
        {
            if (product.Expected.Beneficial is null || product.Expected.Beneficial.Count == 0)
                continue;

            var profile = ToProfile(product.Profile);
            var result = _engine.Evaluate(product.Ingredients, profile);
            var beneficialNames = result.Beneficial.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var expected in product.Expected.Beneficial)
            {
                if (!beneficialNames.Contains(expected))
                {
                    var actual = FindActualCategory(result, expected);
                    failures.Add($"[{product.Name}] '{expected}' expected BENEFICIAL but got {actual}");
                }
                else
                {
                    verifiedCount++;
                }
            }
        }

        _output.WriteLine($"[INFO] Đã xác thực thành công {verifiedCount} hoạt chất Beneficial chuẩn y khoa.");
        Assert.True(failures.Count == 0,
            $"[ERROR] Beneficial regression detected:\n{string.Join("\n", failures)}");
    }

    // ─────────────────────────────────────────────────────────────────
    // Regression: Harmful ingredients must stay harmful
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AllExpectedHarmful_AreClassifiedHarmful()
    {
        var failures = new List<string>();
        int verifiedCount = 0;

        foreach (var product in Products)
        {
            if (product.Expected.Harmful is null || product.Expected.Harmful.Count == 0)
                continue;

            var profile = ToProfile(product.Profile);
            var result = _engine.Evaluate(product.Ingredients, profile);
            var harmfulNames = result.Harmful.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var expected in product.Expected.Harmful)
            {
                if (!harmfulNames.Contains(expected))
                {
                    var actual = FindActualCategory(result, expected);
                    failures.Add($"[{product.Name}] '{expected}' expected HARMFUL but got {actual}");
                }
                else
                {
                    verifiedCount++;
                }
            }
        }

        _output.WriteLine($"[INFO] Đã xác thực thành công {verifiedCount} hoạt chất Harmful chuẩn y khoa.");
        Assert.True(failures.Count == 0,
            $"[ERROR] Harmful regression detected:\n{string.Join("\n", failures)}");
    }

    // ─────────────────────────────────────────────────────────────────
    // Guard: MustNotBe constraints (prevents dangerous misclassification)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void MustNotBe_ConstraintsAreRespected()
    {
        var failures = new List<string>();
        int verifiedCount = 0;

        foreach (var product in Products)
        {
            if (product.Expected.MustNotBe is null || product.Expected.MustNotBe.Count == 0)
                continue;

            var profile = ToProfile(product.Profile);
            var result = _engine.Evaluate(product.Ingredients, profile);

            foreach (var (ingredientName, forbiddenCategory) in product.Expected.MustNotBe)
            {
                var actual = FindActualCategory(result, ingredientName);
                if (actual.Equals(forbiddenCategory, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"[{product.Name}] '{ingredientName}' MUST NOT BE {forbiddenCategory} but IS {actual}");
                }
                else
                {
                    verifiedCount++;
                }
            }
        }

        _output.WriteLine($"[INFO] Đã xác thực thành công {verifiedCount} ràng buộc an toàn MustNotBe.");
        Assert.True(failures.Count == 0,
            $"[ERROR] MustNotBe violation detected:\n{string.Join("\n", failures)}");
    }

    // ─────────────────────────────────────────────────────────────────
    // Coverage: Report match rate across golden dataset
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void MatchRate_MeetsMinimumThreshold()
    {
        int totalIngredients = 0;
        int matchedIngredients = 0;

        foreach (var product in Products)
        {
            var profile = ToProfile(product.Profile);
            var result = _engine.Evaluate(product.Ingredients, profile);

            totalIngredients += product.Ingredients.Count;
            matchedIngredients += result.Beneficial.Count + result.Harmful.Count;
        }

        var matchRate = totalIngredients == 0 ? 0 : (double)matchedIngredients / totalIngredients * 100;
        var diff = BaselineMatchRate - matchRate;

        if (diff > 3.0)
        {
            // 🔴 ERROR (FAIL CI): Sụt giảm nghiêm trọng (>3% absolute)
            Assert.Fail($"[ERROR] Match rate sụt giảm nghiêm trọng: {matchRate:F1}% vs Baseline: {BaselineMatchRate:F1}% (Giảm {diff:F1}% > 3%). Matched: {matchedIngredients}/{totalIngredients}");
        }
        else if (diff > 0.0)
        {
            // 🟡 WARN: Sụt giảm nhẹ (0.5% - 3%)
            _output.WriteLine($"[WARN] Phát hiện dịch chuyển nhẹ (Soft Drift): Match rate {matchRate:F1}% vs Baseline: {BaselineMatchRate:F1}% (Giảm {diff:F1}%). Matched: {matchedIngredients}/{totalIngredients}");
        }
        else
        {
            // 🟢 INFO: Giữ vững hoặc tăng match rate
            _output.WriteLine($"[INFO] Match rate tối ưu: {matchRate:F1}% vs Baseline: {BaselineMatchRate:F1}% (Tăng/Đạt {Math.Abs(diff):F1}%). Matched: {matchedIngredients}/{totalIngredients}");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    private static NormalizedSkinProfile ToProfile(GoldenProfile p) => new(
        SkinType: p.SkinType,
        Concerns: p.Concerns ?? [],
        Sensitivity: p.Sensitivity ?? SensitivityLevels.Low,
        LastAnalyzedAt: DateTime.UtcNow.AddDays(-7),
        IsStale: false
    );

    private static string FindActualCategory(CompatibilityResult result, string name)
    {
        if (result.Beneficial.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return "BENEFICIAL";
        if (result.Harmful.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return "HARMFUL";
        if (result.Neutral.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return "NEUTRAL";
        return "NOT_FOUND";
    }
}

// ─────────────────────────────────────────────────────────────────
// JSON deserialization models
// ─────────────────────────────────────────────────────────────────

public sealed class GoldenDataset
{
    public string Description { get; set; } = "";
    public string Version { get; set; } = "";
    public List<GoldenProduct> Products { get; set; } = [];
}

public sealed class GoldenProduct
{
    public string Name { get; set; } = "";
    public GoldenProfile Profile { get; set; } = new();
    public List<string> Ingredients { get; set; } = [];
    public GoldenExpected Expected { get; set; } = new();
    public string? Comment { get; set; }
}

public sealed class GoldenProfile
{
    public string SkinType { get; set; } = "normal";
    public List<string>? Concerns { get; set; }
    public string? Sensitivity { get; set; }
}

public sealed class GoldenExpected
{
    public List<string>? Beneficial { get; set; }
    public List<string>? Harmful { get; set; }
    public Dictionary<string, string>? MustNotBe { get; set; }
}
