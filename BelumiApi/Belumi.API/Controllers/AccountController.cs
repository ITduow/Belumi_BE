using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(IUserInteractionService userInteractionService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await userInteractionService.GetMeAsync(User.GetUserId(), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("beauty-profile")]
    public async Task<IActionResult> UpdateBeautyProfile(BeautyProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await userInteractionService.UpdateBeautyProfileAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(CancellationToken cancellationToken)
    {
        if (HttpContext.Items.TryGetValue("ResizedAvatarStream", out var streamObj) &&
            streamObj is MemoryStream stream)
        {
            var fileName = HttpContext.Items["ResizedAvatarFileName"] as string ?? "avatar_resized.jpg";
            
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            
            var avatarUrl = $"/uploads/{fileName}";
            var userId = User.GetUserId();
            await userInteractionService.UpdateAvatarAsync(userId, avatarUrl, cancellationToken);
            
            return Ok(new { avatarUrl });
        }
        
        return BadRequest("Không tìm thấy file ảnh hợp lệ hoặc chưa qua xử lý.");
    }
}
