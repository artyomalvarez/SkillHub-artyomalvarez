using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Interfaces.External;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Infrastructure.Identity;
using SkillHub.Infrastructure.Persistence;
using SkillHub.Infrastructure.Persistence.Repositories;
using SkillHub.Infrastructure.Services;

namespace SkillHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 1. Configuración de Base de Datos PostgreSQL con ApplicationDbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
        });

        // 2. Configuración de ASP.NET Core Identity usando ApplicationDbContext
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. Persistencia (Repositorios y Unit of Work)
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWorkshopRepository, WorkshopRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

        // 4. Identity & JWT
        services.AddScoped<JwtGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // 5. Servicios Externos
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IExcelService, ExcelService>();

        return services;
    }
}
