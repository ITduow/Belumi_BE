using System.Text.Json;
using System.Text.Json.Serialization;
using Belumi.Core.DTOs.Gemini;
using Microsoft.Extensions.Logging;

namespace Belumi.Infrastructure.Services;

public sealed class AdviceRuleEngine
{
    private const int MaxAdvice = 5;
    private const int MaxWarnings = 3;
    private const int MaxRoutine = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<AdviceRuleEngine> _logger;
    private readonly Lazy<IReadOnlyList<AdviceRule>> _rules;

    public AdviceRuleEngine(ILogger<AdviceRuleEngine> logger)
    {
        _logger = logger;
        _rules = new Lazy<IReadOnlyList<AdviceRule>>(LoadRules);
    }

    public AdviceRuleOutput Evaluate(SkinAnalysisResult result, SkinAnalysisContext context)
    {
        var facts = RuleFacts.From(result, context);
        var matched = _rules.Value
            .Where(rule => rule.Conditions != null && IsMatch(rule.Conditions, facts))
            .OrderByDescending(rule => PriorityRank(rule.Priority))
            .ThenBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var adviceList = matched
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Advice))
            .Select(rule => new SkinAdviceDto
            {
                Concern = MapCategoryToConcern(rule.Category),
                Content = rule.Advice!.Trim(),
                Priority = Normalize(rule.Priority)
            })
            .DistinctBy(a => a.Content, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAdvice)
            .ToList();

        var routineSteps = new List<SkinRoutineStepDto>();
        var routineStrings = DistinctTake(matched.Select(rule => rule.Routine), MaxRoutine);
        foreach (var rStr in routineStrings)
        {
            routineSteps.AddRange(ParseRoutineString(rStr));
        }
        
        // Basic grouping and re-indexing
        routineSteps = GroupAndSortRoutine(routineSteps);

        var warningsList = matched
            .Where(rule => PriorityRank(rule.Priority) >= PriorityRank("medium") || IsConflict(rule))
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Warning))
            .Select(rule => new SkinWarningDto
            {
                Content = rule.Warning!.Trim(),
                Priority = Normalize(rule.Priority) == "high" || IsConflict(rule) ? "high" : "medium",
                Source = rule.Category
            })
            .DistinctBy(w => w.Content, StringComparer.OrdinalIgnoreCase)
            .Take(MaxWarnings)
            .ToList();

        return new AdviceRuleOutput(adviceList, routineSteps, warningsList);
    }

    /// <summary>
    /// Returns raw matched rules (no string parsing) for AI synthesis.
    /// </summary>
    public List<MatchedRuleSummary> GetMatchedRawRules(SkinAnalysisResult result, SkinAnalysisContext context)
    {
        var facts = RuleFacts.From(result, context);
        return _rules.Value
            .Where(rule => rule.Conditions != null && IsMatch(rule.Conditions, facts))
            .OrderByDescending(rule => PriorityRank(rule.Priority))
            .ThenBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase)
            .Take(10) // cap to avoid token overflow
            .Select(rule => new MatchedRuleSummary(
                MapCategoryToConcern(rule.Category),
                Normalize(rule.Priority),
                rule.Advice?.Trim(),
                rule.Routine?.Trim(),
                rule.Warning?.Trim()
            ))
            .ToList();
    }

    private static string MapCategoryToConcern(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "acne" => "Mụn",
            "oiliness_pore" => "Dầu & Lỗ chân lông",
            "pigmentation" => "Sắc tố",
            "sensitivity" => "Nhạy cảm",
            "aging" => "Lão hóa",
            "goals_conflict" => "Xung đột mục tiêu",
            _ => "Tổng quan"
        };
    }

    private static List<SkinRoutineStepDto> ParseRoutineString(string routineStr)
    {
        var steps = new List<SkinRoutineStepDto>();
        if (string.IsNullOrWhiteSpace(routineStr)) return steps;

        // Extract AM and PM sections using regex-like scan
        var amIdx = routineStr.IndexOf("AM:", StringComparison.OrdinalIgnoreCase);
        var pmIdx = routineStr.IndexOf("PM:", StringComparison.OrdinalIgnoreCase);

        if (amIdx == -1 && pmIdx == -1)
        {
            // No period marker: split by → only (not by '-' to avoid breaking "8-12 tuần")
            var parts = SplitByArrow(routineStr);
            for (int i = 0; i < parts.Count; i++)
                steps.Add(new SkinRoutineStepDto { Period = "ANY", Step = i + 1, Content = parts[i] });
            return steps;
        }

        string? amPart = null;
        string? pmPart = null;

        if (amIdx != -1 && pmIdx != -1)
        {
            if (amIdx < pmIdx)
            {
                amPart = routineStr.Substring(amIdx + 3, pmIdx - amIdx - 3);
                pmPart = routineStr.Substring(pmIdx + 3);
            }
            else
            {
                pmPart = routineStr.Substring(pmIdx + 3, amIdx - pmIdx - 3);
                amPart = routineStr.Substring(amIdx + 3);
            }
        }
        else if (amIdx != -1)
        {
            amPart = routineStr.Substring(amIdx + 3);
        }
        else if (pmIdx != -1)
        {
            pmPart = routineStr.Substring(pmIdx + 3);
        }

        if (!string.IsNullOrWhiteSpace(amPart))
        {
            var parts = SplitByArrow(amPart);
            for (int i = 0; i < parts.Count; i++)
                steps.Add(new SkinRoutineStepDto { Period = "AM", Step = i + 1, Content = parts[i] });
        }

        if (!string.IsNullOrWhiteSpace(pmPart))
        {
            var parts = SplitByArrow(pmPart);
            for (int i = 0; i < parts.Count; i++)
                steps.Add(new SkinRoutineStepDto { Period = "PM", Step = i + 1, Content = parts[i] });
        }

        return steps;
    }

    /// <summary>
    /// Split only on → arrow. Each segment is trimmed and trailing period/comma removed.
    /// Falls back to splitting on ". " (period+space) if no arrows found.
    /// Does NOT split on '-' to preserve ranges like "8-12 tuần".
    /// </summary>
    private static List<string> SplitByArrow(string text)
    {
        var trimmed = text.Trim().TrimEnd('.');
        
        if (trimmed.Contains('→'))
        {
            return trimmed
                .Split('→', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().TrimEnd('.').Trim())
                .Where(s => s.Length > 1)
                .ToList();
        }

        // No arrows: keep as single step (don't split on ". " — may cause false splits)
        return trimmed.Length > 1 ? [trimmed] : [];
    }

    private static List<SkinRoutineStepDto> GroupAndSortRoutine(List<SkinRoutineStepDto> steps)
    {
        var result = new List<SkinRoutineStepDto>();
        var grouped = steps.GroupBy(s => s.Period);
        
        foreach (var group in grouped)
        {
            // Simple deduplication based on content similarity or just distinct
            var distinctSteps = group
                .Select(s => s.Content.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
                
            for (int i = 0; i < distinctSteps.Count; i++)
            {
                result.Add(new SkinRoutineStepDto 
                { 
                    Period = group.Key, 
                    Step = i + 1, 
                    Content = distinctSteps[i],
                    Category = DetermineCategory(distinctSteps[i])
                });
            }
        }
        
        return result.OrderBy(s => s.Period).ThenBy(s => s.Step).ToList();
    }

    private static string DetermineCategory(string content)
    {
        var lower = content.ToLowerInvariant();
        if (lower.Contains("sữa rửa mặt") || lower.Contains("làm sạch") || lower.Contains("gel rửa mặt") || lower.Contains("cleanser")) return "Làm sạch";
        if (lower.Contains("toner") || lower.Contains("nước hoa hồng")) return "Toner";
        if (lower.Contains("serum") || lower.Contains("bha") || lower.Contains("aha") || lower.Contains("niacinamide") || lower.Contains("retin") || lower.Contains("chấm mụn")) return "Treatment";
        if (lower.Contains("dưỡng ẩm") || lower.Contains("kem dưỡng") || lower.Contains("cream") || lower.Contains("lotion")) return "Dưỡng ẩm";
        if (lower.Contains("chống nắng") || lower.Contains("spf")) return "Chống nắng";
        return "Khác";
    }

    private IReadOnlyList<AdviceRule> LoadRules()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "advice-rules.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Advice rules file was not found at {Path}", path);
            return [];
        }

        try
        {
            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<AdviceRuleDocument>(stream, JsonOptions);
            var rules = document?.Rules?
                .Where(rule => rule.Conditions != null)
                .ToList() ?? [];

            _logger.LogInformation("Loaded {Count} skin advice rules", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load skin advice rules from {Path}", path);
            return [];
        }
    }

    private static bool IsMatch(RuleCondition condition, RuleFacts facts)
    {
        if (condition.Rules is { Count: > 0 })
        {
            var logic = Normalize(condition.Logic);
            return logic == "or"
                ? condition.Rules.Any(rule => IsMatch(rule, facts))
                : condition.Rules.All(rule => IsMatch(rule, facts));
        }

        if (string.IsNullOrWhiteSpace(condition.Field) || string.IsNullOrWhiteSpace(condition.Op))
        {
            return false;
        }

        var actual = facts.Get(condition.Field);
        var expectedValues = ReadExpectedValues(condition.Value);
        var op = Normalize(condition.Op);

        return op switch
        {
            "eq" => actual.Values.Any(value => expectedValues.Contains(value, StringComparer.OrdinalIgnoreCase)),
            "in" => actual.Values.Any(value => expectedValues.Contains(value, StringComparer.OrdinalIgnoreCase)),
            "contains" => actual.IsCollection &&
                actual.Values.Any(value => expectedValues.Contains(value, StringComparer.OrdinalIgnoreCase)),
            "contains_any" => actual.IsCollection &&
                actual.Values.Any(value => expectedValues.Contains(value, StringComparer.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static List<string> ReadExpectedValues(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray()
                .Select(ReadString)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .ToList(),
            JsonValueKind.String => [value.GetString() ?? string.Empty],
            JsonValueKind.Number => [value.ToString()],
            JsonValueKind.True => ["true"],
            JsonValueKind.False => ["false"],
            _ => []
        };
    }

    private static string? ReadString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static List<string> DistinctTake(IEnumerable<string?> values, int take)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    private static int PriorityRank(string? priority)
    {
        return Normalize(priority) switch
        {
            "high" => 3,
            "cao" => 3,
            "medium" => 2,
            "trung bình" => 2,
            "trung binh" => 2,
            "low" => 1,
            "thấp" => 1,
            "thap" => 1,
            _ => 0
        };
    }

    private static bool IsConflict(AdviceRule rule)
        => string.Equals(rule.Category, "Goals_Conflict", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private sealed record RuleFacts(Dictionary<string, FactValue> Values)
    {
        public FactValue Get(string field)
            => Values.TryGetValue(field, out var value) ? value : FactValue.Empty;

        public static RuleFacts From(SkinAnalysisResult result, SkinAnalysisContext context)
        {
            var skinType = string.IsNullOrWhiteSpace(context.SkinType)
                ? string.Empty
                : context.SkinType;

            var values = new Dictionary<string, FactValue>(StringComparer.OrdinalIgnoreCase)
            {
                ["skinType"] = FactValue.Single(skinType),
                ["skin_type"] = FactValue.Single(skinType),
                ["skinSensitivity"] = FactValue.Single(context.SkinSensitivity),
                ["skin_sensitivity"] = FactValue.Single(context.SkinSensitivity),
                ["skinGoals"] = FactValue.Collection(context.SkinGoals),
                ["skin_goals"] = FactValue.Collection(context.SkinGoals),
                ["avoidedIngredients"] = FactValue.Collection(context.AvoidedIngredients),
                ["avoided_ingredients"] = FactValue.Collection(context.AvoidedIngredients),
                ["budgetRange"] = FactValue.Single(context.BudgetRange),
                ["budget_range"] = FactValue.Single(context.BudgetRange),
                ["acne_level"] = FactValue.Single(result.AcneLevel),
                ["acne_types"] = FactValue.Collection(result.AcneTypes),
                ["oiliness_level"] = FactValue.Single(result.OilinessLevel),
                ["oiliness_zones"] = FactValue.Collection(result.OilinessZones),
                ["pore_visibility_level"] = FactValue.Single(result.PoreVisibilityLevel),
                ["pigmentation_level"] = FactValue.Single(result.PigmentationLevel),
                ["skin_tone_evenness_level"] = FactValue.Single(result.SkinToneEvennessLevel),
                ["visible_redness_level"] = FactValue.Single(result.VisibleRednessLevel),
                ["visible_wrinkle_level"] = FactValue.Single(result.VisibleWrinkleLevel)
            };

            return new RuleFacts(values);
        }
    }

    private sealed record FactValue(IReadOnlyCollection<string> Values, bool IsCollection)
    {
        public static readonly FactValue Empty = new([], false);

        public static FactValue Single(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? Empty
                : new FactValue([value.Trim()], false);

        public static FactValue Collection(IEnumerable<string>? values)
            => new(
                (values ?? [])
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .ToList(),
                true);
    }

    private sealed record AdviceRuleDocument
    {
        [JsonPropertyName("rules")]
        public List<AdviceRule>? Rules { get; init; }
    }

    private sealed record AdviceRule
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("advice")]
        public string? Advice { get; init; }

        [JsonPropertyName("routine")]
        public string? Routine { get; init; }

        [JsonPropertyName("warning")]
        public string? Warning { get; init; }

        [JsonPropertyName("priority")]
        public string? Priority { get; init; }

        [JsonPropertyName("conditions")]
        public RuleCondition? Conditions { get; init; }
    }

    private sealed record RuleCondition
    {
        [JsonPropertyName("logic")]
        public string? Logic { get; init; }

        [JsonPropertyName("rules")]
        public List<RuleCondition>? Rules { get; init; }

        [JsonPropertyName("field")]
        public string? Field { get; init; }

        [JsonPropertyName("op")]
        public string? Op { get; init; }

        [JsonPropertyName("value")]
        public JsonElement Value { get; init; }
    }
}

public sealed record AdviceRuleOutput(
    List<SkinAdviceDto> Advice,
    List<SkinRoutineStepDto> Routine,
    List<SkinWarningDto> Warnings);

/// <summary>Raw matched rule data — passed to AI for synthesis.</summary>
public sealed record MatchedRuleSummary(
    string Concern,
    string Priority,
    string? Advice,
    string? Routine,
    string? Warning);

