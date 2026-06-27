using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Belumi.API.Controllers;
using Belumi.Core.DTOs.Gemini;
using Belumi.Core.Entities;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Endpoints;

public static class SkinEndpoints
{
    private static readonly string[] ValidSkinTypes =
        ["oily", "dry", "combination", "normal", "sensitive"];

    public static IEndpointRouteBuilder MapSkinEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/skin").WithTags("Skin Analysis");

        group.MapPost("/analyze", async (
            [FromForm] string skin_type,
            ISkinAnalysisService service,
            BelumiDbContext db,
            ClaimsPrincipal user,
            ILogger<ISkinAnalysisService> logger,
            IFormFile? image = null,
            [FromForm] string? image_base64 = null) =>
        {
            var normalizedType = skin_type?.ToLower().Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedType) || !ValidSkinTypes.Contains(normalizedType))
                return Results.BadRequest(new
                {
                    message = $"skin_type không hợp lệ. Các giá trị hợp lệ: {string.Join(", ", ValidSkinTypes)}"
                });

            byte[]? imageBytes = null;

            if (image is { Length: > 0 })
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                imageBytes = ms.ToArray();
                logger.LogInformation(
                    "Received file upload: {FileName} ({Size} bytes)",
                    image.FileName,
                    imageBytes.Length);
            }
            else if (!string.IsNullOrWhiteSpace(image_base64))
            {
                try
                {
                    var base64 = image_base64.Contains(',')
                        ? image_base64.Split(',')[1]
                        : image_base64;

                    imageBytes = Convert.FromBase64String(base64);
                    logger.LogInformation("Received base64 image ({Size} bytes)", imageBytes.Length);
                }
                catch
                {
                    return Results.BadRequest(new { message = "image_base64 không hợp lệ." });
                }
            }

            if (imageBytes == null || imageBytes.Length == 0)
                return Results.BadRequest(new
                {
                    message = "Vui lòng cung cấp ảnh qua field 'image' (file) hoặc 'image_base64' (base64 string)."
                });

            var result = await service.AnalyzeAsync(imageBytes, normalizedType);
            await SaveSuccessfulAnalysisAsync(db, user, result, normalizedType, logger);

            return result.Status switch
            {
                "success" => Results.Ok(result),
                "retake_required" => Results.UnprocessableEntity(result),
                _ => Results.Problem(
                    detail: result.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Lỗi hệ thống khi phân tích ảnh")
            };
        })
        .WithName("AnalyzeSkin")
        .WithSummary("Phân tích ảnh da mặt")
        .WithDescription(
            "Nhận ảnh khuôn mặt và loại da đã biết từ quiz. " +
            "Trả về các vấn đề da nhìn thấy được, mức độ mụn, loại mụn, độ tin cậy và mô tả tiếng Việt.\n\n" +
            "**Cách gửi ảnh (chọn 1):**\n" +
            "- `image`: file upload (JPEG/PNG, max 5MB)\n" +
            "- `image_base64`: chuỗi base64, có thể kèm prefix `data:image/jpeg;base64,...`")
        .Produces<AnalysisResponse>(StatusCodes.Status200OK)
        .Produces<AnalysisResponse>(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .DisableAntiforgery();

        group.MapGet("/history/me", async (
            BelumiDbContext db,
            ClaimsPrincipal user,
            int page = 1,
            int pageSize = 20) =>
        {
            var userId = user.GetUserId();
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = db.SkinAnalyses.AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.AnalyzedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SkinAnalysisHistoryItem(
                    x.Id,
                    x.SkinType,
                    x.Concerns,
                    x.Recommendations,
                    x.Score,
                    x.AnalyzedAt,
                    x.AiResult,
                    x.RecommendedIngredients,
                    x.AvoidIngredients))
                .ToListAsync();

            return Results.Ok(new SkinAnalysisHistoryResponse(items, total, page, pageSize));
        })
        .RequireAuthorization()
        .WithName("GetMySkinAnalysisHistory")
        .WithSummary("Láº¥y lá»‹ch sá»­ phÃ¢n tÃ­ch da cá»§a user hiá»‡n táº¡i");

        group.MapGet("/history/me/{id:guid}", async (
            Guid id,
            BelumiDbContext db,
            ClaimsPrincipal user) =>
        {
            var userId = user.GetUserId();
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var item = await db.SkinAnalyses.AsNoTracking()
                .Where(x => x.Id == id && x.UserId == userId)
                .Select(x => new SkinAnalysisHistoryItem(
                    x.Id,
                    x.SkinType,
                    x.Concerns,
                    x.Recommendations,
                    x.Score,
                    x.AnalyzedAt,
                    x.AiResult,
                    x.RecommendedIngredients,
                    x.AvoidIngredients))
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .RequireAuthorization()
        .WithName("GetMySkinAnalysisHistoryDetail")
        .WithSummary("Láº¥y chi tiáº¿t má»™t káº¿t quáº£ phÃ¢n tÃ­ch da cá»§a user hiá»‡n táº¡i");

        return app;
    }

    private static async Task SaveSuccessfulAnalysisAsync(
        BelumiDbContext db,
        ClaimsPrincipal user,
        AnalysisResponse response,
        string skinType,
        ILogger logger)
    {
        var userId = user.GetUserId();
        if (userId == Guid.Empty || response is not { Status: "success", Result: not null })
        {
            return;
        }

        var exists = await db.Users.FindAsync(userId) != null;
        if (!exists)
        {
            logger.LogWarning("Skip saving skin analysis because user {UserId} was not found", userId);
            return;
        }

        var result = response.Result;
        db.SkinAnalyses.Add(new SkinAnalysis
        {
            UserId = userId,
            SkinType = skinType,
            Concerns = BuildConcerns(result),
            AiResult = JsonSerializer.Serialize(result),
            RecommendedIngredients = "[]",
            AvoidIngredients = "[]",
            Recommendations = BuildRecommendations(result),
            Score = (int)Math.Round(Math.Clamp(result.Confidence, 0, 1) * 100),
            AnalyzedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private static string BuildConcerns(SkinAnalysisResult result)
    {
        var concerns = new[]
        {
            result.AcneLevel != "none" ? $"acne:{result.AcneLevel}" : null,
            result.PigmentationLevel is "medium" or "high" ? $"pigmentation:{result.PigmentationLevel}" : null,
            result.PoreVisibilityLevel is "medium" or "high" ? $"pores:{result.PoreVisibilityLevel}" : null,
            result.VisibleRednessLevel is "medium" or "high" ? $"redness:{result.VisibleRednessLevel}" : null,
            result.OilinessLevel is "medium" or "high" ? $"oiliness:{result.OilinessLevel}" : null,
            result.VisibleWrinkleLevel is "medium" or "high" ? $"wrinkles:{result.VisibleWrinkleLevel}" : null,
            result.SkinToneEvennessLevel is "medium" or "high" ? $"uneven_tone:{result.SkinToneEvennessLevel}" : null
        };

        return string.Join(", ", concerns.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string BuildRecommendations(SkinAnalysisResult result)
    {
        var parts = new[]
        {
            result.Description
        };

        return string.Join("\n", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}

public sealed record SkinAnalysisHistoryResponse(
    IReadOnlyCollection<SkinAnalysisHistoryItem> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record SkinAnalysisHistoryItem(
    Guid Id,
    string SkinType,
    string Concerns,
    string Recommendations,
    int Score,
    DateTime AnalyzedAt,
    string? AiResult,
    string? RecommendedIngredients,
    string? AvoidIngredients);
