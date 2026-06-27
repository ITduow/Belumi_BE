using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Belumi.Core.DTOs;

// ─── Request ──────────────────────────────────────────────────────────────────

public sealed record SubmitQuizRequest
{
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    /// <summary>female | male | other</summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    /// <summary>under18 | 18-22 | 23-26 | over27</summary>
    [JsonPropertyName("age_group")]
    public string? AgeGroup { get; init; }

    /// <summary>normal | dry | combination | oily</summary>
    [JsonPropertyName("skin_type")]
    public string? SkinType { get; init; }

    /// <summary>Max 3 values: hydration | brightening | pore_control | dark_spot | anti_aging | soothing</summary>
    [JsonPropertyName("skin_goals")]
    public List<string>? SkinGoals { get; init; }

    /// <summary>stable | mild | sensitive</summary>
    [JsonPropertyName("skin_sensitivity")]
    public string? SkinSensitivity { get; init; }

    /// <summary>Max 3 values: fragrance | alcohol | paraben | mineral_oil | retinol | none + optional custom string</summary>
    [JsonPropertyName("avoided_ingredients")]
    public List<string>? AvoidedIngredients { get; init; }

    /// <summary>under200k | 200-300k | 300-500k | 500k-1m | over1m</summary>
    [JsonPropertyName("budget_range")]
    public string? BudgetRange { get; init; }

    /// <summary>Free-text, current products the user is using</summary>
    [JsonPropertyName("current_products")]
    public string? CurrentProducts { get; init; }
}

// ─── Response ─────────────────────────────────────────────────────────────────

public sealed record QuizResponse
{
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("age_group")]
    public string? AgeGroup { get; init; }

    [JsonPropertyName("skin_type")]
    public string? SkinType { get; init; }

    [JsonPropertyName("skin_goals")]
    public List<string> SkinGoals { get; init; } = new();

    [JsonPropertyName("skin_sensitivity")]
    public string? SkinSensitivity { get; init; }

    [JsonPropertyName("avoided_ingredients")]
    public List<string> AvoidedIngredients { get; init; } = new();

    [JsonPropertyName("budget_range")]
    public string? BudgetRange { get; init; }

    [JsonPropertyName("current_products")]
    public string? CurrentProducts { get; init; }

    [JsonPropertyName("quiz_completed_at")]
    public DateTime? QuizCompletedAt { get; init; }
}

public sealed record QuizStatusResponse
{
    [JsonPropertyName("quiz_completed")]
    public bool QuizCompleted { get; init; }

    [JsonPropertyName("quiz_completed_at")]
    public DateTime? QuizCompletedAt { get; init; }
}
