using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Belumi.Core.DTOs.Gemini;
using Belumi.Core.Interfaces;
using System;

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
                logger.LogInformation("Received file upload: {FileName} ({Size} bytes)",
                    image.FileName, imageBytes.Length);
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

            return result.Status switch
            {
                "success"         => Results.Ok(result),
                "retake_required" => Results.UnprocessableEntity(result),
                _                 => Results.Problem(
                    detail:     result.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title:      "Lỗi hệ thống khi phân tích ảnh")
            };
        })
        .WithName("AnalyzeSkin")
        .WithSummary("Phân tích ảnh da mặt")
        .WithDescription(
            "Nhận ảnh khuôn mặt và loại da đã biết từ quiz. " +
            "Trả về các vấn đề da nhìn thấy được, điểm tổng thể (0-100) và mô tả tiếng Việt.\n\n" +
            "**Cách gửi ảnh (chọn 1):**\n" +
            "- `image`: file upload (JPEG/PNG, max 5MB)\n" +
            "- `image_base64`: chuỗi base64, có thể kèm prefix `data:image/jpeg;base64,...`")
        .Produces<AnalysisResponse>(StatusCodes.Status200OK)
        .Produces<AnalysisResponse>(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .DisableAntiforgery();

        return app;
    }
}
