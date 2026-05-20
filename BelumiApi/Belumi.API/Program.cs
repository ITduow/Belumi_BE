<<<<<<< Updated upstream
using System.Text;
=======
>>>>>>> Stashed changes
using Belumi.API.Common;
using Belumi.Application.Validators;
using Belumi.Infrastructure.Data;
using Belumi.Infrastructure;
using FluentValidation;
<<<<<<< Updated upstream
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
=======
using Microsoft.AspNetCore.Authentication;
>>>>>>> Stashed changes

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

<<<<<<< Updated upstream
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
=======
builder.Services.AddValidatorsFromAssemblyContaining<FirebaseLoginRequestValidator>();
>>>>>>> Stashed changes
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BelumiDbContext>();
    await BelumiSeedData.SeedAsync(db);
}

app.Run();
