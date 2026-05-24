using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Belumi.Core.DTOs.Gemini;

public class SkinAnalysisResult
{
    [JsonPropertyName("acne_level")]
    public string AcneLevel { get; set; } = string.Empty;

    [JsonPropertyName("dark_spots")]
    public bool DarkSpots { get; set; }

    [JsonPropertyName("enlarged_pores")]
    public bool EnlargedPores { get; set; }

    [JsonPropertyName("redness")]
    public bool Redness { get; set; }

    [JsonPropertyName("uneven_tone")]
    public bool UnevenTone { get; set; }

    [JsonPropertyName("top_concerns")]
    public List<string> TopConcerns { get; set; } = new();

    [JsonPropertyName("overall_score")]
    public int OverallScore { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("skin_condition")]
    public string SkinCondition { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("advice")]
    public List<string> Advice { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
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
