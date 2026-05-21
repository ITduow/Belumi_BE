using Belumi.API.Common;
using Belumi.API.Middleware;
using Belumi.Application.Validators;
using Belumi.Infrastructure;
using Belumi.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining<FirebaseLoginRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("BelumiApp", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddAuthentication(BelumiBearerAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BelumiBearerAuthenticationHandler>(
        BelumiBearerAuthenticationHandler.SchemeName,
        options => { });
builder.Services.AddAuthorization();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Task 18: Bật Swagger cả production để test dễ dàng
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles(); // Task 20: Enable serving uploaded static files under wwwroot
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
