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



    [JsonPropertyName("recommended_ingredients")]
    public List<IngredientRecommendation> RecommendedIngredients { get; set; } = new();

    [JsonPropertyName("avoid_or_professional_only")]
    public List<IngredientRecommendation> AvoidOrProfessionalOnly { get; set; } = new();
}

public class IngredientRecommendation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("source_ids")]
    public List<string> SourceIds { get; set; } = new();
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
