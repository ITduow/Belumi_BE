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

        // Pick exactly 1 routine (the most relevant one), not merge all.
        // Priority: condition-specific (Acne, Pigmentation, Aging, Dullness, etc.) > skinType base routine
        var conditionCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Acne", "Pigmentation_Evenness", "Wrinkle_AntiAging", "Dullness", "Redness_Sensitivity"
        };

        var bestRoutineRule = matched.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.Routine) &&
            conditionCategories.Contains(r.Category));

        bestRoutineRule ??= matched.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.Routine) &&
            r.Category.StartsWith("Basic Routine", StringComparison.OrdinalIgnoreCase));

        bestRoutineRule ??= matched.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Routine));

        var routineSteps = new List<SkinRoutineStepDto>();
        if (bestRoutineRule?.Routine != null)
        {
            routineSteps.AddRange(ParseRoutineString(bestRoutineRule.Routine));
        }

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

        // Try splitting by " / " or " /" which separates Morning and Night in the Vietnamese format
        string[] parts = routineStr.Split(new[] { " / Tối:", " / PM:" }, StringSplitOptions.None);
        string amStr = parts[0];
        string? pmStr = parts.Length > 1 ? parts[1] : null;

        // If no " / " separator, try to manually find the indexes
        if (pmStr == null)
        {
            var amIdx = routineStr.IndexOf("AM:", StringComparison.OrdinalIgnoreCase);
            if (amIdx == -1) amIdx = routineStr.IndexOf("Sáng:", StringComparison.OrdinalIgnoreCase);

            var pmIdx = routineStr.IndexOf("PM:", StringComparison.OrdinalIgnoreCase);
            if (pmIdx == -1) pmIdx = routineStr.IndexOf("Tối:", StringComparison.OrdinalIgnoreCase);

            if (amIdx == -1 && pmIdx == -1)
            {
                var parsedParts = SplitIntoSteps(routineStr);
                for (int i = 0; i < parsedParts.Count; i++)
                    steps.Add(new SkinRoutineStepDto { Period = "ANY", Step = i + 1, Content = parsedParts[i] });
                return steps;
            }

            if (amIdx != -1 && pmIdx != -1)
            {
                if (amIdx < pmIdx)
                {
                    amStr = routineStr.Substring(amIdx, pmIdx - amIdx);
                    pmStr = routineStr.Substring(pmIdx);
                }
                else
                {
                    pmStr = routineStr.Substring(pmIdx, amIdx - pmIdx);
                    amStr = routineStr.Substring(amIdx);
                }
            }
            else if (amIdx != -1)
            {
                amStr = routineStr.Substring(amIdx);
            }
            else if (pmIdx != -1)
            {
                amStr = "";
                pmStr = routineStr.Substring(pmIdx);
            }
        }

        // Clean up prefix for AM
        amStr = System.Text.RegularExpressions.Regex.Replace(amStr, @"^(AM:|Sáng:)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!string.IsNullOrWhiteSpace(amStr))
        {
            var amParsed = SplitIntoSteps(amStr);
            for (int i = 0; i < amParsed.Count; i++)
                steps.Add(new SkinRoutineStepDto { Period = "AM", Step = i + 1, Content = amParsed[i] });
        }

        // Clean up prefix for PM (if it wasn't split by " / Tối:" which already removed it)
        if (pmStr != null)
        {
            pmStr = System.Text.RegularExpressions.Regex.Replace(pmStr, @"^(PM:|Tối:)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(pmStr))
            {
                var pmParsed = SplitIntoSteps(pmStr);
                for (int i = 0; i < pmParsed.Count; i++)
                    steps.Add(new SkinRoutineStepDto { Period = "PM", Step = i + 1, Content = pmParsed[i] });
            }
        }

        return steps;
    }

    /// <summary>
    /// Split by → or by numbered list "1. ", "2. "
    /// </summary>
    private static List<string> SplitIntoSteps(string text)
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

        // Split by "1. ", "2. ", etc., using regex. 
        // e.g. "1. Rửa mặt: ... 2. Toner: ..." -> ["Rửa mặt: ...", "Toner: ..."]
        var matches = System.Text.RegularExpressions.Regex.Split(trimmed, @"\b\d+\.\s");
        
        var result = matches
            .Select(s => s.Trim().TrimEnd('.').Trim())
            .Where(s => s.Length > 1)
            .ToList();
            
        if (result.Count > 0) return result;

        return trimmed.Length > 1 ? [trimmed] : [];
    }

    public static List<SkinRoutineStepDto> GroupAndSortRoutine(List<SkinRoutineStepDto> steps)
    {
        var result = new List<SkinRoutineStepDto>();
        var grouped = steps.GroupBy(s => s.Period);
        
        foreach (var group in grouped)
        {
            var distinctSteps = group
                .Select(s => s.Content.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var filteredSteps = new List<string>();
            var seenCategories = new HashSet<string>();

            foreach (var step in distinctSteps)
            {
                var cat = DetermineCategory(step);
                // Allow multiple treatments, but deduplicate basic steps like Cleanser, Toner, Moisturizer, Sunscreen
                if (cat == "Treatment" || cat == "Khác" || seenCategories.Add(cat))
                {
                    filteredSteps.Add(step);
                }
            }
                
            for (int i = 0; i < filteredSteps.Count; i++)
            {
                result.Add(new SkinRoutineStepDto 
                { 
                    Period = group.Key, 
                    Step = i + 1, 
                    Content = filteredSteps[i],
                    Category = DetermineCategory(filteredSteps[i])
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

