using System.Text;
using Belumi.API.Middleware;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("BelumiApp", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "BelumiBeautyLocalDevelopmentKeyMustBeLong";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Task 18: Bật Swagger cả production (Railway) để test dễ dàng
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseImageResize(); // Task 20: Middleware resize avatar
app.UseCors("BelumiApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BelumiDbContext>();
    await db.Database.MigrateAsync(); // Task 18: Auto-apply migrations khi start
    await BelumiSeedData.SeedAsync(db);

    // Task 18: Import từ CSV nếu có file (chạy được cả local lẫn Railway)
    var csvPath = Path.Combine(AppContext.BaseDirectory, "ingredients.csv");
    await Belumi.Infrastructure.Services.CsvIngredientImporter.ImportFromCsvAsync(db, csvPath);
}

app.Run();
