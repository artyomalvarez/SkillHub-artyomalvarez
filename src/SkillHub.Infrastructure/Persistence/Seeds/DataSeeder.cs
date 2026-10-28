using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence.Seeds;

public static class DataSeeder
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n--> [SEEDER] ==========================================");
        Console.WriteLine("--> [SEEDER] Iniciando proceso de siembra de datos...");

        try
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Roles Base
            string[] roles = ["Staff", "Participant"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"--> [SEEDER] Rol creado: {role}");
                    }
                }
            }

            // 2. Localizar el archivo Excel con rutas alternativas seguras
            var baseDir = AppContext.BaseDirectory;
            var currentDir = Directory.GetCurrentDirectory();

            string[] candidatePaths =
            [
                Path.Combine(baseDir, "SkillHub_Data.xlsx"),
                Path.Combine(currentDir, "SkillHub_Data.xlsx"),
                Path.Combine(currentDir, "src", "SkillHub.Api", "SkillHub_Data.xlsx")
            ];

            var filePath = candidatePaths.FirstOrDefault(File.Exists);

            if (string.IsNullOrEmpty(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("--> [SEEDER ADVERTENCIA] No se encontró 'SkillHub_Data.xlsx' en ninguna de las siguientes rutas:");
                foreach (var p in candidatePaths)
                {
                    Console.WriteLine($"    - {p}");
                }
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"--> [SEEDER] Archivo Excel encontrado en: {filePath}");

            // 3. Verificar si ya existen talleres en la BD
            if (await context.Workshops.AnyAsync())
            {
                var existingCount = await context.Workshops.CountAsync();
                Console.WriteLine($"--> [SEEDER] La tabla Workshops ya contiene {existingCount} registros. Omitiendo importación para evitar duplicados.");
                Console.WriteLine("--> [SEEDER] ==========================================\n");
                return;
            }

            using var workbook = new XLWorkbook(filePath);

            // 4. Importar Instructores (Rol Staff)
            var instructorsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Instructors", StringComparison.OrdinalIgnoreCase));
            if (instructorsSheet != null)
            {
                int instructorsCreated = 0;
                foreach (var row in instructorsSheet.RowsUsed().Skip(1))
                {
                    var fullName = row.Cell(1).GetString().Trim();
                    var email = row.Cell(2).GetString().Trim();
                    var password = row.Cell(3).GetString().Trim();
                    var specialty = row.Cell(4).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    var existingUser = await userManager.FindByEmailAsync(email);
                    if (existingUser == null)
                    {
                        var instructor = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = fullName,
                            Specialty = specialty,
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(instructor, password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(instructor, "Staff");
                            instructorsCreated++;
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                            Console.WriteLine($"--> [SEEDER ERROR] No se pudo crear el instructor {email}: {errors}");
                        }
                    }
                }
                Console.WriteLine($"--> [SEEDER] Instructores procesados exitosamente ({instructorsCreated} nuevos creados).");
            }
            else
            {
                Console.WriteLine("--> [SEEDER ADVERTENCIA] No se encontró la hoja 'Instructors' en el Excel.");
            }

            // 5. Importar Participantes (Rol Participant)
            var participantsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Participants", StringComparison.OrdinalIgnoreCase));
            if (participantsSheet != null)
            {
                int participantsCreated = 0;
                foreach (var row in participantsSheet.RowsUsed().Skip(1))
                {
                    var fullName = row.Cell(1).GetString().Trim();
                    var email = row.Cell(2).GetString().Trim();
                    var password = row.Cell(3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    var existingUser = await userManager.FindByEmailAsync(email);
                    if (existingUser == null)
                    {
                        var participant = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = fullName,
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(participant, password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(participant, "Participant");
                            participantsCreated++;
                        }
                        else
                        {
                            var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                            Console.WriteLine($"--> [SEEDER ERROR] No se pudo crear el participante {email}: {errors}");
                        }
                    }
                }
                Console.WriteLine($"--> [SEEDER] Participantes procesados exitosamente ({participantsCreated} nuevos creados).");
            }
            else
            {
                Console.WriteLine("--> [SEEDER ADVERTENCIA] No se encontró la hoja 'Participants' en el Excel.");
            }

            // 6. Importar Talleres (Workshops)
            var workshopsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Workshops", StringComparison.OrdinalIgnoreCase));
            if (workshopsSheet != null)
            {
                int workshopsAdded = 0;

                foreach (var row in workshopsSheet.RowsUsed().Skip(1))
                {
                    var code = row.Cell(1).GetString().Trim();
                    var title = row.Cell(2).GetString().Trim();
                    var description = row.Cell(3).GetString().Trim();
                    var instructorEmail = row.Cell(4).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
                        continue;

                    // Parseo ultra-seguro de fechas (manejando OADate numérico y formatos texto)
                    var StartDateTime = DateTime.SpecifyKind(row.Cell(5).GetDateTime(), DateTimeKind.Utc);
                    var EndDateTime = DateTime.SpecifyKind(row.Cell(6).GetDateTime(), DateTimeKind.Utc);

                    // Parseo seguro de capacidad
                    int capacity = 10;
                    if (row.Cell(7).TryGetValue<int>(out var cap) && cap > 0)
                    {
                        capacity = cap;
                    }
                    else if (int.TryParse(row.Cell(7).GetString().Trim(), out var parsedCap) && parsedCap > 0)
                    {
                        capacity = parsedCap;
                    }

                    // Buscar o crear instructor de respaldo para asegurar que el taller nunca se pierda
                    var instructor = await userManager.FindByEmailAsync(instructorEmail);
                    if (instructor == null)
                    {
                        Console.WriteLine($"--> [SEEDER] Instructor '{instructorEmail}' no existía. Creando instructor automático...");
                        instructor = new ApplicationUser
                        {
                            UserName = instructorEmail,
                            Email = instructorEmail,
                            FullName = "Instructor " + code,
                            Specialty = "General",
                            EmailConfirmed = true
                        };
                        var createInstructorResult = await userManager.CreateAsync(instructor, "Instructor2026*");
                        if (createInstructorResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(instructor, "Staff");
                        }
                    }

                    var workshop = new Workshop
                    {
                        Id = Guid.NewGuid(),
                        Code = code,
                        Title = title,
                        Description = description,
                        InstructorId = instructor.Id,
                        StartDateTime = StartDateTime,
                        EndDateTime = EndDateTime,
                        Capacity = capacity
                    };

                    context.Workshops.Add(workshop);
                    workshopsAdded++;
                }

                if (workshopsAdded > 0)
                {
                    Console.WriteLine($"--> [SEEDER] Guardando {workshopsAdded} talleres en PostgreSQL...");
                    await context.SaveChangesAsync();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"--> [SEEDER ÉXITO] ¡Se guardaron exitosamente {workshopsAdded} talleres en la Base de Datos!");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("--> [SEEDER] No se encontraron filas de talleres válidas para insertar.");
                }
            }
            else
            {
                Console.WriteLine("--> [SEEDER ADVERTENCIA] No se encontró la hoja 'Workshops' en el Excel.");
            }

            Console.WriteLine("--> [SEEDER] ==========================================\n");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[SEEDER FATAL] Error no controlado: {ex.Message}");
            Console.WriteLine($"Detalles: {ex}\n");
            Console.ResetColor();
            throw; // Re-lanzar para que Program.cs también lo capture
        }
    }

    /// <summary>
    /// Convierte celdas de ClosedXML a DateTime con DateTimeKind.Utc,
    /// soportando tipos nativos de fecha, valores numéricos OADate de Excel (ej: 46361.33)
    /// y cadenas de texto en formato invariante o local.
    /// </summary>
    private static DateTime ParseUtcDateTime(IXLCell cell)
    {
        DateTime result;

        // 1. Tipo nativo DateTime de ClosedXML
        if (cell.DataType == XLDataType.DateTime)
        {
            result = cell.GetDateTime();
        }
        // 2. Extracción directa de fecha
        else if (cell.TryGetValue(out DateTime dt))
        {
            result = dt;
        }
        // 3. Extracción de número OADate (número de serie de Excel, ej: 46361.3333)
        else if (cell.TryGetValue(out double oaDate))
        {
            result = DateTime.FromOADate(oaDate);
        }
        // 4. Si viene como cadena numérica de OADate
        else
        {
            var text = cell.GetString().Trim();
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDouble))
            {
                result = DateTime.FromOADate(parsedDouble);
            }
            else if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                result = parsedDate;
            }
            else if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var localDate))
            {
                result = localDate;
            }
            else
            {
                result = DateTime.Parse(text);
            }
        }

        // Es indispensable forzar DateTimeKind.Utc para que PostgreSQL (Npgsql) acepte la columna 'timestamp with time zone'
        return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }
}
