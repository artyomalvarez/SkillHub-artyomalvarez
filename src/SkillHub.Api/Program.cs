using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SkillHub.Api.Middleware;
using SkillHub.Application;
using SkillHub.Infrastructure;
using SkillHub.Infrastructure.Persistence.Seeds;

var builder = WebApplication.CreateBuilder(args);

// 1. Inyección de capas Application e Infrastructure
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Controladores y OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // Generador nativo de .NET

// 3. Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 4. Configuración de Autenticación JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["Key"] ?? "SkillHubSuperSecretKeyForDevelopmentPurposes2026!";
var issuer = jwtSection["Issuer"] ?? "SkillHubApi";
var audience = jwtSection["Audience"] ?? "SkillHubWeb";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ==========================================
// AQUÍ SE CONSTRUYE LA APP
var app = builder.Build(); 
// ==========================================

// 5. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // <-- Corregido aquí (ya no es UseSwagger)
    app.MapScalarApiReference();
}

app.UseCors("AllowAll");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 6. Seed de Datos inicial
using (var scope = app.Services.CreateScope())
{
    try
    {
        DataSeeder.Initialize(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la ejecución del seeder de base de datos.");
    }
}

app.Run();