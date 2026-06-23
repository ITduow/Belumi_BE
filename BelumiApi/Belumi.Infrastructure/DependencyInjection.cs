using Belumi.Application.Abstractions;
using Belumi.Core.Interfaces;
using Belumi.Infrastructure.AI;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayOS;

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
        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = configuration["OpenAI:ApiKey"];
            options.Model = configuration["OpenAI:Model"] ?? options.Model;
        });
        services.Configure<InciApiOptions>(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("INCI_API_KEY") ?? configuration["InciApi:ApiKey"];
            options.BaseUrl = configuration["InciApi:BaseUrl"] ?? options.BaseUrl;
            if (int.TryParse(configuration["InciApi:CacheDays"], out var cacheDays))
            {
                options.CacheDays = cacheDays;
            }
        });
        services.AddSingleton<FirebaseAdminAppFactory>();
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var clientId = config["PayOS:ClientId"] ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID");
            var apiKey = config["PayOS:ApiKey"] ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY");
            var checksumKey = config["PayOS:ChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(checksumKey))
            {
                clientId = "placeholder_id";
                apiKey = "placeholder_key";
                checksumKey = "placeholder_checksum";
            }

            return new PayOSClient(clientId, apiKey, checksumKey);
        });
        services.AddMemoryCache();
        services.AddScoped<ChatbotRequestContext>();
        services.AddScoped<ChatToolCallTracker>();
        services.AddScoped<BelumiChatPlugin>();
        services.AddScoped<IOpenAiChatService, OpenAiChatService>();
        services.AddHttpClient<IInciApiClient, InciApiClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InciApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            }
        });
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
