using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Belumi.API.Middleware;

/// <summary>
/// Task 20: Middleware resize ảnh avatar upload về tối đa 256x256
/// Áp dụng cho route POST /api/account/avatar
/// </summary>
public sealed class ImageResizeMiddleware(RequestDelegate next)
{
    private const int MaxWidthPx = 256;
    private const int MaxHeightPx = 256;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public async Task InvokeAsync(HttpContext context)
    {
        // Chỉ áp dụng cho upload avatar
        if (!context.Request.Path.StartsWithSegments("/api/account/avatar") ||
            context.Request.Method != "POST")
        {
            await next(context);
            return;
        }

        if (!context.Request.HasFormContentType)
        {
            await next(context);
            return;
        }

        var form = await context.Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            await next(context);
            return;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(new { message = "File quá lớn. Tối đa 5MB." });
            return;
        }

        // Resize ảnh bằng SixLabors.ImageSharp
        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        if (image.Width > MaxWidthPx || image.Height > MaxHeightPx)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(MaxWidthPx, MaxHeightPx),
                Mode = ResizeMode.Max
            }));
        }

        var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream);
        outputStream.Position = 0;

        // Thay thế form data với ảnh đã resize
        context.Items["ResizedAvatarStream"] = outputStream;
        context.Items["ResizedAvatarFileName"] = Path.GetFileNameWithoutExtension(file.FileName) + "_resized.jpg";

        await next(context);
    }
}

public static class ImageResizeMiddlewareExtensions
{
    public static IApplicationBuilder UseImageResize(this IApplicationBuilder app)
        => app.UseMiddleware<ImageResizeMiddleware>();
}
