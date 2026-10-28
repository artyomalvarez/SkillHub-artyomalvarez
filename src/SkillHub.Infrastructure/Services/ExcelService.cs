using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using SkillHub.Application.Dtos.Excel;
using SkillHub.Application.Interfaces.External;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Services;

public class ExcelService : IExcelService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcelService(
        UserManager<ApplicationUser> userManager,
        IWorkshopRepository workshopRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _workshopRepository = workshopRepository;
        _enrollmentRepository = enrollmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportResultDto> ImportEnrollmentsAsync(Stream fileStream, string staffId)
    {
        var errors = new List<ImportErrorDto>();
        int successCount = 0;

        try
        {
            // ClosedXML reads the workbook into memory. The worksheet evaluation starts at row 2 to bypass headers.
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                errors.Add(new ImportErrorDto(0, "El archivo Excel no contiene hojas de cálculo."));
                return new ImportResultDto(0, errors);
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow is null || lastRow.RowNumber() < 2)
            {
                errors.Add(new ImportErrorDto(0, "El archivo está vacío o solo contiene la fila de encabezados."));
                return new ImportResultDto(0, errors);
            }

            for (int rowNumber = 2; rowNumber <= lastRow.RowNumber(); rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                var participantEmail = row.Cell(1).GetString().Trim();
                var workshopCode = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(participantEmail) && string.IsNullOrWhiteSpace(workshopCode))
                {
                    continue; // Fila vacía al final
                }

                if (string.IsNullOrWhiteSpace(participantEmail))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "El correo del participante es obligatorio."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(workshopCode))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "El código del taller es obligatorio."));
                    continue;
                }

                // 1. Validar que el participante exista y tenga el rol Participant
                var user = await _userManager.FindByEmailAsync(participantEmail);
                if (user is null)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"No se encontró ningún participante registrado con el correo '{participantEmail}'."));
                    continue;
                }

                var isParticipant = await _userManager.IsInRoleAsync(user, "Participant");
                if (!isParticipant)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"El usuario con correo '{participantEmail}' no cuenta con el rol de participante."));
                    continue;
                }

                // 2. Validar que el taller exista por su código
                var workshop = await _workshopRepository.GetByCodeAsync(workshopCode.ToUpperInvariant());
                if (workshop is null)
                {
                    // Fallback con código tal como viene
                    workshop = await _workshopRepository.GetByCodeAsync(workshopCode);
                }

                if (workshop is null)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"No se encontró ningún taller con el código '{workshopCode}'."));
                    continue;
                }

                // 3. Validar que el taller sea futuro
                if (workshop.StartDateTime <= DateTime.UtcNow)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"El taller '{workshopCode}' ya ha iniciado o finalizado."));
                    continue;
                }

                // 4. Validar cupos disponibles (solo inscripciones activas)
                var activeCount = await _enrollmentRepository.GetActiveEnrollmentsCountByWorkshopIdAsync(workshop.Id);
                if (!workshop.HasAvailableCapacity(activeCount))
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"El taller '{workshopCode}' no cuenta con cupos disponibles."));
                    continue;
                }

                // 5. Validar que el participante no tenga una inscripción activa duplicada
                var isAlreadyEnrolled = await _enrollmentRepository.HasActiveEnrollmentAsync(workshop.Id, user.Id);
                if (isAlreadyEnrolled)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"El participante ya cuenta con una inscripción activa en el taller '{workshopCode}'."));
                    continue;
                }

                // 6. Validar cruce de horarios con otras inscripciones activas
                var hasOverlap = await _enrollmentRepository.HasOverlappingActiveEnrollmentAsync(user.Id, workshop.StartDateTime, workshop.EndDateTime);
                if (hasOverlap)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"El taller '{workshopCode}' presenta cruce de horarios con otra inscripción activa del participante."));
                    continue;
                }

                // 7. Registrar inscripción y persistir
                try
                {
                    var enrollment = new Enrollment
                    {
                        Id = Guid.NewGuid(),
                        WorkshopId = workshop.Id,
                        ParticipantId = user.Id,
                        Status = EnrollmentStatus.Active,
                        EnrolledAt = DateTime.UtcNow
                    };

                    await _enrollmentRepository.AddAsync(enrollment);
                    await _unitOfWork.SaveChangesAsync();
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Error al guardar la inscripción: {ex.Message}"));
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ImportErrorDto(0, $"Error al procesar el archivo Excel: {ex.Message}"));
        }

        return new ImportResultDto(successCount, errors);
    }
}
