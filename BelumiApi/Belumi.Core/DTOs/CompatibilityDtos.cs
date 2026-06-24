namespace Belumi.Core.DTOs;

// ─────────────────────────────────────────────────────────────────────
// Normalized Skin Profile (read from latest SkinAnalysis)
// ─────────────────────────────────────────────────────────────────────

public sealed record NormalizedSkinProfile(
    string SkinType,
    List<string> Concerns,
    string Sensitivity,
    DateTime LastAnalyzedAt,
    bool IsStale
);

// ─────────────────────────────────────────────────────────────────────
// Compatibility Engine DTOs
// ─────────────────────────────────────────────────────────────────────

public sealed record CompatibilityResult(
    int Score,
    string Status,
    List<CompatibilityItem> Beneficial,
    List<CompatibilityItem> Harmful,
    List<CompatibilityItem> Neutral
);

public sealed record CompatibilityItem(
    string Name,
    string Reason,
    string PersonalReason
);

// ─────────────────────────────────────────────────────────────────────
// Personalized assessment for single ingredient detail
// ─────────────────────────────────────────────────────────────────────

public sealed record PersonalizedAssessment(
    string Status,
    List<string> Reasons
);

// ─────────────────────────────────────────────────────────────────────
// General ingredient info (goodFor / avoidFor — Sprint 2 ready)
// ─────────────────────────────────────────────────────────────────────

public sealed record IngredientGeneralInfo(
    List<string> GoodFor,
    List<string> AvoidFor
);
