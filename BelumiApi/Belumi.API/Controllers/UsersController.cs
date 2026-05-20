using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(BelumiDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Where(x => x.Id == User.GetUserId())
            .Select(x => new
            {
                x.Id,
                x.FirebaseUid,
                x.Email,
                x.FullName,
                x.Phone,
                x.AvatarUrl,
                x.Role,
                x.SubscriptionPlan,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.GetUserId()], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.FullName : request.FullName.Trim();
        user.Phone = request.Phone ?? user.Phone;
        user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(user);
    }
}

public sealed record UserUpdateRequest(string? FullName, string? Phone, string? AvatarUrl);
