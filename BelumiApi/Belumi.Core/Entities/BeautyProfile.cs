using System;
using System.Collections.Generic;

namespace Belumi.Core.Entities;

/// <summary>
/// 1-1 onboarding quiz result linked to a User.
/// </summary>
public sealed class BeautyProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // ── Q1 ───────────────────────────────────────────────────────────────────
    /// <summary>Nickname the user wants to be called (Q1)</summary>
    public string? Nickname { get; set; }

    // ── Q2 ───────────────────────────────────────────────────────────────────
    /// <summary>female | male | other (Q2)</summary>
    public string? Gender { get; set; }

    // ── Q3 ───────────────────────────────────────────────────────────────────
    /// <summary>under18 | 18-22 | 23-26 | over27 (Q3)</summary>
    public string? AgeGroup { get; set; }

    // ── Q4 ───────────────────────────────────────────────────────────────────
    /// <summary>normal | dry | combination | oily (Q4)</summary>
    public string? SkinType { get; set; }

    // ── Q5 ───────────────────────────────────────────────────────────────────
    /// <summary>Up to 3 goals: hydration | brightening | pore_control | dark_spot | anti_aging | soothing (Q5)</summary>
    public string? SkinGoals { get; set; }  // JSON array stored as string

    // ── Q6 ───────────────────────────────────────────────────────────────────
    /// <summary>stable | mild | sensitive (Q6)</summary>
    public string? SkinSensitivity { get; set; }

    // ── Q7 ───────────────────────────────────────────────────────────────────
    /// <summary>Up to 3: fragrance | alcohol | paraben | mineral_oil | retinol | none | custom text (Q7)</summary>
    public string? AvoidedIngredients { get; set; }  // JSON array stored as string

    // ── Q8 ───────────────────────────────────────────────────────────────────
    /// <summary>under200k | 200-300k | 300-500k | 500k-1m | over1m (Q8)</summary>
    public string? BudgetRange { get; set; }

    // ── Q9 ───────────────────────────────────────────────────────────────────
    /// <summary>Free-text list of current products the user is using (Q9)</summary>
    public string? CurrentProducts { get; set; }

    // ── Legacy / kept for backward compat ────────────────────────────────────
    /// <summary>Free-text skin concerns (legacy, preserved)</summary>
    public string? SkinConcerns { get; set; }

    /// <summary>Free-text allergies (legacy, preserved)</summary>
    public string? Allergies { get; set; }

    // ── Quiz completion ───────────────────────────────────────────────────────
    /// <summary>Null = quiz not done. Non-null = quiz completed at this timestamp.</summary>
    public DateTime? QuizCompletedAt { get; set; }
}
