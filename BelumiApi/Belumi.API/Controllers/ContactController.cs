using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController(IContentService contentService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(ContactRequestDto request, CancellationToken cancellationToken)
    {
        var contact = await contentService.CreateContactAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { contact.Id }, contact);
    }
}
