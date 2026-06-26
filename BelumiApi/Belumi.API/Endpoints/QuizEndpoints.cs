using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Endpoints;

public static class QuizEndpoints
{
    // ── Allowed enum values ──────────────────────────────────────────────────
    private static readonly HashSet<string> ValidGenders        = ["female", "male", "other"];
    private static readonly HashSet<string> ValidAgeGroups      = ["under18", "18-22", "23-26", "over27"];
    private static readonly HashSet<string> ValidSkinTypes      = ["normal", "dry", "combination", "oily"];
    private static readonly HashSet<string> ValidGoals          = ["hydration", "brightening", "pore_control", "dark_spot", "anti_aging", "soothing"];
    private static readonly HashSet<string> ValidSensitivities  = ["stable", "mild", "sensitive"];
    private static readonly HashSet<string> ValidBudgets        = ["under200k", "200-300k", "300-500k", "500k-1m", "over1m"];
    private static readonly HashSet<string> ValidIngredients    = ["fragrance", "alcohol", "paraben", "mineral_oil", "retinol", "none"];

    public static IEndpointRouteBuilder MapQuizEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/profile/quiz")
            .WithTags("Onboarding Quiz")
            .RequireAuthorization();

        // GET /api/profile/quiz/status — kiểm tra đã hoàn thành quiz chưa
        group.MapGet("/status", async (ClaimsPrincipal user, BelumiDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var profile = await db.BeautyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            return Results.Ok(new QuizStatusResponse
            {
                QuizCompleted    = profile?.QuizCompletedAt != null,
                QuizCompletedAt  = profile?.QuizCompletedAt
            });
        })
        .WithName("GetQuizStatus");

        // GET /api/profile/quiz — lấy dữ liệu quiz đã điền
        group.MapGet("/", async (ClaimsPrincipal user, BelumiDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var profile = await db.BeautyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            if (profile == null)
                return Results.NotFound(new { message = "Chưa có dữ liệu quiz." });

            return Results.Ok(MapToResponse(profile));
        })
        .WithName("GetQuiz");

        // POST /api/profile/quiz — submit lần đầu
        group.MapPost("/", async (SubmitQuizRequest req, ClaimsPrincipal user, BelumiDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var validationError = Validate(req);
            if (validationError != null)
                return Results.BadRequest(new { message = validationError });

            var existing = await db.BeautyProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            if (existing != null)
                return Results.Conflict(new { message = "Quiz đã được hoàn thành. Dùng PUT để cập nhật." });

            var profile = new BeautyProfile
            {
                UserId               = userId.Value,
                Nickname             = req.Nickname?.Trim(),
                Gender               = req.Gender,
                AgeGroup             = req.AgeGroup,
                SkinType             = req.SkinType,
                SkinGoals            = SerializeList(req.SkinGoals),
                SkinSensitivity      = req.SkinSensitivity,
                AvoidedIngredients   = SerializeList(req.AvoidedIngredients),
                BudgetRange          = req.BudgetRange,
                CurrentProducts      = req.CurrentProducts?.Trim(),
                QuizCompletedAt      = DateTime.UtcNow
            };

            db.BeautyProfiles.Add(profile);
            await db.SaveChangesAsync();

            return Results.Created($"/api/profile/quiz", MapToResponse(profile));
        })
        .WithName("SubmitQuiz");

        // PUT /api/profile/quiz — cập nhật lại sau
        group.MapPut("/", async (SubmitQuizRequest req, ClaimsPrincipal user, BelumiDbContext db) =>
        {
            var userId = GetUserId(user);
            if (userId == null) return Results.Unauthorized();

            var validationError = Validate(req);
            if (validationError != null)
                return Results.BadRequest(new { message = validationError });

            var profile = await db.BeautyProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId.Value);

            if (profile == null)
                return Results.NotFound(new { message = "Chưa có quiz. Dùng POST để tạo mới." });

            profile.Nickname            = req.Nickname?.Trim();
            profile.Gender              = req.Gender;
            profile.AgeGroup            = req.AgeGroup;
            profile.SkinType            = req.SkinType;
            profile.SkinGoals           = SerializeList(req.SkinGoals);
            profile.SkinSensitivity     = req.SkinSensitivity;
            profile.AvoidedIngredients  = SerializeList(req.AvoidedIngredients);
            profile.BudgetRange         = req.BudgetRange;
            profile.CurrentProducts     = req.CurrentProducts?.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(MapToResponse(profile));
        })
        .WithName("UpdateQuiz");

        return app;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? Validate(SubmitQuizRequest req)
    {
        if (req.Gender != null && !ValidGenders.Contains(req.Gender))
            return $"gender không hợp lệ. Hợp lệ: {string.Join(", ", ValidGenders)}";

        if (req.AgeGroup != null && !ValidAgeGroups.Contains(req.AgeGroup))
            return $"age_group không hợp lệ. Hợp lệ: {string.Join(", ", ValidAgeGroups)}";

        if (req.SkinType != null && !ValidSkinTypes.Contains(req.SkinType))
            return $"skin_type không hợp lệ. Hợp lệ: {string.Join(", ", ValidSkinTypes)}";

        if (req.SkinSensitivity != null && !ValidSensitivities.Contains(req.SkinSensitivity))
            return $"skin_sensitivity không hợp lệ. Hợp lệ: {string.Join(", ", ValidSensitivities)}";

        if (req.BudgetRange != null && !ValidBudgets.Contains(req.BudgetRange))
            return $"budget_range không hợp lệ. Hợp lệ: {string.Join(", ", ValidBudgets)}";

        if (req.SkinGoals is { Count: > 3 })
            return "skin_goals tối đa 3 mục.";

        if (req.SkinGoals != null)
        {
            var invalid = req.SkinGoals.FirstOrDefault(g => !ValidGoals.Contains(g));
            if (invalid != null)
                return $"skin_goals giá trị '{invalid}' không hợp lệ. Hợp lệ: {string.Join(", ", ValidGoals)}";
        }

        if (req.AvoidedIngredients is { Count: > 3 })
            return "avoided_ingredients tối đa 3 mục.";

        // avoided_ingredients cho phép custom string nên chỉ kiểm tra độ dài
        if (req.AvoidedIngredients != null)
        {
            var tooLong = req.AvoidedIngredients.FirstOrDefault(i => i.Length > 100);
            if (tooLong != null)
                return "avoided_ingredients: mỗi mục tối đa 100 ký tự.";
        }

        return null;
    }

    private static string? SerializeList(List<string>? list)
        => list is { Count: > 0 } ? JsonSerializer.Serialize(list) : null;

    private static List<string> DeserializeList(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    private static QuizResponse MapToResponse(BeautyProfile profile) => new()
    {
        Nickname            = profile.Nickname,
        Gender              = profile.Gender,
        AgeGroup            = profile.AgeGroup,
        SkinType            = profile.SkinType,
        SkinGoals           = DeserializeList(profile.SkinGoals),
        SkinSensitivity     = profile.SkinSensitivity,
        AvoidedIngredients  = DeserializeList(profile.AvoidedIngredients),
        BudgetRange         = profile.BudgetRange,
        CurrentProducts     = profile.CurrentProducts,
        QuizCompletedAt     = profile.QuizCompletedAt
    };
}
