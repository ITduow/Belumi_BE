using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Belumi.Core.DTOs.Gemini;

public class SkinAnalysisResult
{
    [JsonPropertyName("face_detected")]
    public bool FaceDetected { get; set; }

    [JsonPropertyName("image_subject")]
    public string ImageSubject { get; set; } = "unknown";

    [JsonPropertyName("acne_level")]
    public string AcneLevel { get; set; } = string.Empty;

    [JsonPropertyName("acne_types")]
    public List<string> AcneTypes { get; set; } = new();

    [JsonPropertyName("oiliness_level")]
    public string OilinessLevel { get; set; } = string.Empty;

    [JsonPropertyName("oiliness_zones")]
    public List<string> OilinessZones { get; set; } = new();

    [JsonPropertyName("pore_visibility_level")]
    public string PoreVisibilityLevel { get; set; } = string.Empty;

    [JsonPropertyName("pigmentation_level")]
    public string PigmentationLevel { get; set; } = string.Empty;

    [JsonPropertyName("skin_tone_evenness_level")]
    public string SkinToneEvennessLevel { get; set; } = string.Empty;

    [JsonPropertyName("visible_redness_level")]
    public string VisibleRednessLevel { get; set; } = string.Empty;

    [JsonPropertyName("visible_wrinkle_level")]
    public string VisibleWrinkleLevel { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("skin_condition")]
    public string SkinCondition { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("advice")]
    public List<SkinAdviceDto> Advice { get; set; } = new();

    [JsonPropertyName("routine")]
    public List<SkinRoutineStepDto> Routine { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<SkinWarningDto> Warnings { get; set; } = new();

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } = string.Empty;
}

public class SkinAdviceDto
{
    [JsonPropertyName("concern")]
    public string Concern { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "low";
}

public class SkinRoutineStepDto
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = "AM"; // AM, PM, or ANY

    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class SkinWarningDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "low";

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
}

public sealed record SkinAnalysisContext
{
    public string SkinType { get; init; } = string.Empty;
    public string? SkinSensitivity { get; init; }
    public IReadOnlyCollection<string> SkinGoals { get; init; } = [];
    public IReadOnlyCollection<string> AvoidedIngredients { get; init; } = [];
    public string? BudgetRange { get; init; }
}

public class AnalysisResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public SkinAnalysisResult? Result { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("from_cache")]
    public bool FromCache { get; set; }
}
