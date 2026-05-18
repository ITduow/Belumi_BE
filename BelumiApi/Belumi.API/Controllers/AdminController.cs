using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminController(BelumiDbContext db) : ControllerBase
{
    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts(CancellationToken cancellationToken) =>
        Ok(await db.ContactRequests.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken));

    [HttpPatch("contacts/{id:guid}/status")]
    public async Task<IActionResult> UpdateContactStatus(Guid id, [FromBody] ContactStatus status, CancellationToken cancellationToken)
    {
        var contact = await db.ContactRequests.FindAsync([id], cancellationToken);
        if (contact is null)
        {
            return NotFound();
        }

        contact.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(contact);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(Product product, CancellationToken cancellationToken)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/products/{product.Id}", product);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, Product product, CancellationToken cancellationToken)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        db.Entry(product).State = EntityState.Modified;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(product);
    }
}
