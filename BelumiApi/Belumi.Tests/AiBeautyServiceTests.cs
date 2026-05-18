using Belumi.Core.Entities;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Belumi.Tests;

public class AiBeautyServiceTests
{
    private BelumiDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<BelumiDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        return new BelumiDbContext(options);
    }

    [Fact]
    public async Task Should_Block_CautionIngredients_ForSensitiveSkin()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        
        var sensitiveUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "sensitive@test.com",
            FullName = "Sensitive User",
            PasswordHash = "hash",
            BeautyProfile = new BeautyProfile
            {
                SkinType = "Sensitive",
                SkinConcerns = "Redness",
                Allergies = "None"
            }
        };

        dbContext.Users.Add(sensitiveUser);
        await dbContext.SaveChangesAsync();

        var aiService = new AiBeautyService(dbContext);

        // Act
        var prompt = await aiService.BuildSkinConsultationContextAsync(sensitiveUser.Id);

        // Assert: KHUNG KIỂM THỬ (HARNESS) ĐẢM BẢO AN TOÀN
        Assert.Contains("KHÔNG BAO GIỜ khuyên dùng", prompt);
        Assert.Contains("Salicylic Acid", prompt);
        Assert.Contains("Retinol", prompt);
    }
    
    [Fact]
    public async Task Should_Not_Include_CautionRule_ForNormalSkin()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext();
        
        var normalUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "normal@test.com",
            FullName = "Normal User",
            PasswordHash = "hash",
            BeautyProfile = new BeautyProfile
            {
                SkinType = "Normal",
                SkinConcerns = "None",
                Allergies = "None"
            }
        };

        dbContext.Users.Add(normalUser);
        await dbContext.SaveChangesAsync();

        var aiService = new AiBeautyService(dbContext);

        // Act
        var prompt = await aiService.BuildSkinConsultationContextAsync(normalUser.Id);

        // Assert
        Assert.DoesNotContain("KHÔNG BAO GIỜ khuyên dùng", prompt);
        Assert.DoesNotContain("Salicylic Acid", prompt);
    }
}
