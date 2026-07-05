using Belumi.Core.DTOs.Gemini;
using Belumi.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Belumi.Tests;

public sealed class AdviceRuleEngineTests
{
    [Fact]
    public void Evaluate_ReturnsAdviceRoutineAndWarnings_ForMatchedRules()
    {
        var engine = new AdviceRuleEngine(NullLogger<AdviceRuleEngine>.Instance);
        var result = new SkinAnalysisResult
        {
            AcneLevel = "moderate",
            AcneTypes = ["papule_like"],
            OilinessLevel = "high",
            OilinessZones = ["forehead", "nose"],
            PoreVisibilityLevel = "medium",
            PigmentationLevel = "low",
            SkinToneEvennessLevel = "low",
            VisibleRednessLevel = "high",
            VisibleWrinkleLevel = "medium"
        };
        var context = new SkinAnalysisContext
        {
            SkinType = "sensitive",
            SkinGoals = ["anti_aging"],
            AvoidedIngredients = ["fragrance"],
            BudgetRange = "under200k"
        };

        var output = engine.Evaluate(result, context);

        Assert.NotEmpty(output.Advice);
        Assert.NotEmpty(output.Routine);
        Assert.Contains(output.Warnings, warning => warning.Content.Contains("XUNG", StringComparison.OrdinalIgnoreCase));
    }
}
