using Belumi.Application.Abstractions;
using Belumi.Core.DTOs;
using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Belumi.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController(ICatalogService catalogService, BelumiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> Get([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetProductsAsync(categoryId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(id, cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost("import-excel")]
    [AllowAnonymous]
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Please upload a valid Excel file." });

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = file.OpenReadStream();
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];
        if (worksheet == null) return BadRequest(new { message = "No worksheet found" });

        int rowCount = worksheet.Dimension.Rows;

        var productsToAdd = new List<Product>();
        var categories = await dbContext.Categories.ToListAsync(cancellationToken);
        
        for (int row = 2; row <= rowCount; row++)
        {
            var brand = worksheet.Cells[row, 1].Text?.Trim();
            var name = worksheet.Cells[row, 2].Text?.Trim();
            var categoryName = worksheet.Cells[row, 3].Text?.Trim();
            var ingredients = worksheet.Cells[row, 4].Text?.Trim();
            var url = worksheet.Cells[row, 5].Text?.Trim();
            var imageUrl = worksheet.Cells[row, 6].Text?.Trim();

            if (string.IsNullOrWhiteSpace(name)) continue;

            var category = categories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (category == null && !string.IsNullOrWhiteSpace(categoryName))
            {
                category = new Category { Name = categoryName };
                dbContext.Categories.Add(category);
                categories.Add(category);
            }

            var product = new Product
            {
                Brand = string.IsNullOrWhiteSpace(brand) ? "Belumi" : brand,
                Name = name,
                Category = category,
                Ingredients = ingredients ?? string.Empty,
                ImageUrl = imageUrl,
                ThumbnailUrl = imageUrl,
                SourceUrl = url,
                Description = string.Empty,
                Price = 0,
                IsActive = true
            };
            productsToAdd.Add(product);
        }

        dbContext.Products.AddRange(productsToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = $"Imported {productsToAdd.Count} products successfully." });
    }

    [HttpGet("recommend-by-skin")]
    [AllowAnonymous]
    public async Task<IActionResult> RecommendProducts([FromQuery] string skinType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(skinType))
            return BadRequest(new { message = "skinType is required" });

        skinType = skinType.ToLower().Trim();

        var dbIngredients = await dbContext.Ingredients
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var goodIngredients = dbIngredients
            .Where(i => !string.IsNullOrWhiteSpace(i.SuitableSkin) && i.SuitableSkin.ToLower().Contains(skinType))
            .Select(i => i.NameInc.ToLower().Trim())
            .ToHashSet();

        var badIngredients = dbIngredients
            .Where(i => !string.IsNullOrWhiteSpace(i.NotForSkin) && i.NotForSkin.ToLower().Contains(skinType))
            .Select(i => i.NameInc.ToLower().Trim())
            .ToHashSet();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var suitableProducts = new List<ProductDto>();
        var unsuitableProducts = new List<ProductDto>();

        foreach (var p in products)
        {
            if (string.IsNullOrWhiteSpace(p.Ingredients)) continue;

            var productIngredients = p.Ingredients.Split(',')
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            bool isBad = productIngredients.Any(i => badIngredients.Contains(i));
            bool isGood = productIngredients.Any(i => goodIngredients.Contains(i));

            var dto = new ProductDto(
                p.Id, 
                p.Name, 
                p.Description, 
                p.Ingredients, 
                p.Benefits, 
                p.Price, 
                p.ThumbnailUrl, 
                p.CategoryId, 
                null, 
                [],
                p.Brand,
                p.ImageUrl);

            if (isBad)
            {
                unsuitableProducts.Add(dto);
            }
            else if (isGood)
            {
                suitableProducts.Add(dto);
            }
        }

        return Ok(new
        {
            SuitableProducts = suitableProducts.Take(20),
            UnsuitableProducts = unsuitableProducts.Take(20)
        });
    }
}
