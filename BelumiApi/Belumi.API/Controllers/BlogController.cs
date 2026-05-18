using Belumi.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/blogs")]
public sealed class BlogController(IContentService contentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await contentService.GetBlogsAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var post = await contentService.GetBlogAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }
}
