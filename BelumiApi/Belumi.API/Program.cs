using Belumi.API.Common;
using Belumi.API.Endpoints;
using Belumi.Application.Validators;
using Belumi.Infrastructure;
using Belumi.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;

// Load .env từ root hoặc parent directories giống belumi
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BelumiApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSkinEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BelumiDbContext>();
    await BelumiSeedData.SeedAsync(db);
}

app.Run();
