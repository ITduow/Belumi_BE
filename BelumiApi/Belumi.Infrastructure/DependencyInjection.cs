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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var isDev = configuration["ASPNETCORE_ENVIRONMENT"] == "Development" || string.IsNullOrEmpty(configuration["ASPNETCORE_ENVIRONMENT"]);
            if (isDev)
            {
                connectionString = "Host=localhost;Port=5432;Database=belumi;Username=postgres;Password=12345";
            }
            else
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in production environment configuration.");
            }
        }

        services.AddDbContext<BelumiDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<FirebaseAdminAppFactory>();
        services.AddMemoryCache();
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
