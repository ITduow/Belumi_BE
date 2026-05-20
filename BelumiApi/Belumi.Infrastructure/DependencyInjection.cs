using Belumi.Application.Abstractions;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Belumi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Task 18: Hỗ trợ Railway/Render DATABASE_URL (production) hoặc appsettings (local)
        var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        // Nếu không có DATABASE_URL hoặc trống, lấy từ appsettings.json hoặc fallback mặc định
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            rawConnectionString = configuration.GetConnectionString("DefaultConnection");
        }
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            rawConnectionString = "Host=localhost;Port=5432;Database=belumi;Username=postgres;Password=12345";
        }

        // Làm sạch chuỗi kết nối (xóa khoảng trắng thừa, xóa dấu ngoặc kép/ngoặc đơn bọc ngoài nếu có)
        var connectionString = rawConnectionString.Trim().Trim('"', '\'');

        if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
            connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var databaseUri = new Uri(connectionString);
                var userInfo = databaseUri.UserInfo.Split(':');
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = databaseUri.Host;
                var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
                var database = Uri.UnescapeDataString(databaseUri.AbsolutePath.TrimStart('/'));

                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;Trust Server Certificate=true;";
                
                Console.WriteLine($"[DB CONFIG] Parsed URI to Npgsql: Host={host}, Port={port}, Database={database}, Username={username}, SSL=Require");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB ERROR] Failed to parse database URI: {ex.Message}. Fallback to raw connection string.");
            }
        }
        else
        {
            var masked = MaskConnectionString(connectionString);
            Console.WriteLine($"[DB CONFIG] Using standard connection string format: {masked}");
        }

        services.AddDbContext<BelumiDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<FirebaseAdminAppFactory>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISkinAnalysisService, SkinAnalysisService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IAiBeautyService, AiBeautyService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IUserInteractionService, UserInteractionService>();

        return services;
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) return string.Empty;
        try
        {
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var trimmed = parts[i].Trim();
                if (trimmed.StartsWith("Password", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = trimmed.Split('=');
                    if (kv.Length > 1)
                    {
                        parts[i] = $"{kv[0]}=********";
                    }
                }
            }
            return string.Join(";", parts);
        }
        catch
        {
            return "[Masked Connection String]";
        }
    }
}

