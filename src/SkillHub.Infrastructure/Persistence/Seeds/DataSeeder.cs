using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence.Seeds;

public static class DataSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Asegurar que la base de datos esté creada (opcional en migraciones)
        // await context.Database.MigrateAsync();

        // 1. Validar si ya existen usuarios
        if (!await userManager.Users.AnyAsync())
        {
            // 2. Crear los roles base del sistema
            string[] roles = ["Staff", "Participant"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Crear usuario Staff por defecto
            const string staffEmail = "staff@skillhub.com";
            var defaultStaff = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                FullName = "Staff Administrator",
                Specialty = "Plataforma SkillHub",
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(defaultStaff, "Staff2026*");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(defaultStaff, "Staff");
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al inicializar el usuario Staff: {errors}");
            }
        }

        // 4. Estructura preparada para carga inicial desde Excel
        await SeedFromExcelAsync(scope.ServiceProvider);
    }

    public static void Initialize(IServiceProvider serviceProvider)
    {
        InitializeAsync(serviceProvider).GetAwaiter().GetResult();
    }

    private static Task SeedFromExcelAsync(IServiceProvider serviceProvider)
    {
        // TODO: Estructura preparada para importar talleres o inscripciones desde archivo Excel
        // var excelService = serviceProvider.GetService<IExcelService>();
        return Task.CompletedTask;
    }
}
