using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Belumi.Infrastructure.Services;

/// <summary>
/// Task 18: Đọc ingredients.csv và seed vào bảng Ingredients trong DB
/// </summary>
public static class CsvIngredientImporter
{
    public static async Task ImportFromCsvAsync(BelumiDbContext db, string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"[WARN] Không tìm thấy file: {csvPath}. Dùng seed data mặc định.");
            return;
        }

        if (await db.Ingredients.AnyAsync())
        {
            Console.WriteLine("[SKIP] Bảng Ingredients đã có dữ liệu, bỏ qua import CSV.");
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);
        var ingredients = new List<Ingredient>();

        // Bỏ qua dòng header
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',', 3); // Name, Category, ImageUrl
            if (parts.Length < 2) continue;

            var name = parts[0].Trim().Trim('"');
            var category = parts[1].Trim().Trim('"');
            var imageUrl = parts.Length >= 3 ? parts[2].Trim().Trim('"') : null;

            if (string.IsNullOrEmpty(name)) continue;

            ingredients.Add(new Ingredient
            {
                Name = name,
                InciName = name, // CSV dùng INCI name làm Name
                Description = $"Thành phần mỹ phẩm thuộc nhóm {category}.",
                SkinTypes = "All",
                Benefits = category,
                SafetyRating = "Safe"
            });
        }

        if (ingredients.Count == 0) return;

        db.Ingredients.AddRange(ingredients);
        await db.SaveChangesAsync();

        Console.WriteLine($"[OK] Đã import {ingredients.Count} thành phần từ {Path.GetFileName(csvPath)}");
    }
}
