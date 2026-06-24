namespace Belumi.Core;

/// <summary>
/// Normalized skin type values used by the Compatibility Engine.
/// </summary>
public static class SkinTypes
{
    public const string Oily = "oily";
    public const string Dry = "dry";
    public const string Combination = "combination";
    public const string Sensitive = "sensitive";
    public const string Normal = "normal";

    public static readonly string[] All = [Oily, Dry, Combination, Sensitive, Normal];

    /// <summary>
    /// Normalize any skin type string to a standard value.
    /// </summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Normal;

        var lower = value.Trim().ToLowerInvariant();

        if (lower.Contains("oily") || lower.Contains("dầu") || lower.Contains("dau") || lower.Contains("oil"))
            return Oily;
        if (lower.Contains("dry") || lower.Contains("khô") || lower.Contains("kho"))
            return Dry;
        if (lower.Contains("combination") || lower.Contains("hỗn hợp") || lower.Contains("hon hop"))
            return Combination;
        if (lower.Contains("sensitive") || lower.Contains("nhạy") || lower.Contains("nhay"))
            return Sensitive;
        if (lower.Contains("normal") || lower.Contains("thường") || lower.Contains("thuong"))
            return Normal;

        return Normal;
    }
}

/// <summary>
/// Normalized skin concern values used by the Compatibility Engine.
/// </summary>
public static class SkinConcerns
{
    public const string Acne = "acne";
    public const string DarkSpots = "dark_spots";
    public const string Redness = "redness";
    public const string EnlargedPores = "enlarged_pores";
    public const string Dehydration = "dehydration";

    /// <summary>
    /// Parse a concerns string (e.g. "acne:moderate, dark_spots, redness") into a list of normalized concern keys.
    /// </summary>
    public static List<string> Parse(string? concerns)
    {
        if (string.IsNullOrWhiteSpace(concerns)) return [];

        var result = new List<string>();
        var lower = concerns.ToLowerInvariant();

        if (lower.Contains("acne") || lower.Contains("mụn") || lower.Contains("mun"))
            result.Add(Acne);
        if (lower.Contains("dark_spot") || lower.Contains("thâm") || lower.Contains("tham") || lower.Contains("hyperpigmentation"))
            result.Add(DarkSpots);
        if (lower.Contains("redness") || lower.Contains("đỏ") || lower.Contains("do"))
            result.Add(Redness);
        if (lower.Contains("enlarged_pore") || lower.Contains("lỗ chân lông") || lower.Contains("pore"))
            result.Add(EnlargedPores);
        if (lower.Contains("dehydrat") || lower.Contains("khô") || lower.Contains("kho") || lower.Contains("dry"))
            result.Add(Dehydration);

        return result;
    }
}

/// <summary>
/// Normalized sensitivity level values.
/// </summary>
public static class SensitivityLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Low;

        var lower = value.Trim().ToLowerInvariant();

        if (lower.Contains("high") || lower.Contains("cao") || lower.Contains("severe"))
            return High;
        if (lower.Contains("medium") || lower.Contains("moderate") || lower.Contains("trung bình") || lower.Contains("trung binh"))
            return Medium;

        return Low;
    }
}
