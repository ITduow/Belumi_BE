using Belumi.Application.Abstractions;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Belumi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Task 18: Hỗ trợ Railway DATABASE_URL (production) hoặc appsettings (local)
        var connectionString =
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=belumi;Username=postgres;Password=12345";

        if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
        {
            var databaseUri = new Uri(connectionString);
            var userInfo = databaseUri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var host = databaseUri.Host;
            var port = databaseUri.Port;
            var database = databaseUri.AbsolutePath.TrimStart('/');

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;Trust Server Certificate=true;";
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
}
