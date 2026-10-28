using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Dtos.Workshops;
using SkillHub.Application.Interfaces.External;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Entities;
using ApplicationException = SkillHub.Application.Common.Exceptions.ApplicationException;

namespace SkillHub.Application.Services;

public class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        IEnrollmentRepository enrollmentRepository)
    {
        _workshopRepository = workshopRepository;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _enrollmentRepository = enrollmentRepository;
    }

    public Task<WorkshopDto> CreateAsync(CreateWorkshopRequestDto dto, CancellationToken ct = default)
    {
        return CreateWorkshopAsync(dto, ct);
    }

    public async Task<WorkshopDto> CreateWorkshopAsync(CreateWorkshopRequestDto dto, CancellationToken ct = default)
    {
        var instructorId = await _userService.GetUserIdByEmailAsync(dto.InstructorEmail);
        if (string.IsNullOrWhiteSpace(instructorId))
        {
            throw new ApplicationException($"No se encontró ningún instructor registrado con el correo '{dto.InstructorEmail}'.");
        }

        var existingWorkshop = await _workshopRepository.GetByCodeAsync(dto.Code, ct);
        if (existingWorkshop is not null)
        {
            throw new ApplicationException("El código del taller ya existe en la plataforma.");
        }

        // PostgreSQL requires explicit UTC timestamps with time zone. SpecifyKind ensures the timestamp has Utc kind without conversion drift.
        var startDateTimeUtc = DateTime.SpecifyKind(dto.StartDateTime, DateTimeKind.Utc);
        var endDateTimeUtc = DateTime.SpecifyKind(dto.EndDateTime, DateTimeKind.Utc);

        if (startDateTimeUtc <= DateTime.UtcNow)
        {
            throw new ApplicationException("La fecha de inicio debe ser en el futuro.");
        }

        if (startDateTimeUtc >= endDateTimeUtc)
        {
            throw new ApplicationException("La fecha de inicio debe ser anterior a la fecha de finalización.");
        }

        if (dto.Capacity <= 0)
        {
            throw new ApplicationException("La capacidad del taller debe ser mayor a cero.");
        }

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.Trim().ToUpperInvariant(),
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            InstructorId = instructorId,
            StartDateTime = startDateTimeUtc,
            EndDateTime = endDateTimeUtc,
            Capacity = dto.Capacity
        };

        await _workshopRepository.AddAsync(workshop, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Catches any race-condition or concurrent constraint violation on the UNIQUE index for workshop code.
            throw new ApplicationException("El código del taller ya existe en la plataforma.");
        }

        var instructorName = await _userService.GetUserFullNameAsync(instructorId) ?? dto.InstructorEmail;

        return new WorkshopDto(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            instructorName,
            workshop.StartDateTime,
            workshop.EndDateTime,
            workshop.Capacity,
            workshop.Capacity
        );
    }

    public async Task<WorkshopDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var workshop = await _workshopRepository.GetByIdAsync(id, ct);
        if (workshop is null)
        {
            return null;
        }

        var activeCount = await _enrollmentRepository.GetActiveEnrollmentsCountByWorkshopIdAsync(workshop.Id, ct);
        var availableSlots = Math.Max(0, workshop.Capacity - activeCount);
        var instructorName = await _userService.GetUserFullNameAsync(workshop.InstructorId) ?? "Instructor";

        return new WorkshopDto(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            instructorName,
            workshop.StartDateTime,
            workshop.EndDateTime,
            workshop.Capacity,
            availableSlots
        );
    }

    public async Task<IReadOnlyList<WorkshopDto>> GetUpcomingWorkshopsAsync(CancellationToken ct = default)
    {
        var workshops = await _workshopRepository.GetUpcomingAsync(ct);
        var result = new List<WorkshopDto>(workshops.Count);

        foreach (var workshop in workshops)
        {
            var activeCount = await _enrollmentRepository.GetActiveEnrollmentsCountByWorkshopIdAsync(workshop.Id, ct);
            var availableSlots = Math.Max(0, workshop.Capacity - activeCount);
            var instructorName = await _userService.GetUserFullNameAsync(workshop.InstructorId) ?? "Instructor";

            result.Add(new WorkshopDto(
                workshop.Id,
                workshop.Code,
                workshop.Title,
                workshop.Description,
                instructorName,
                workshop.StartDateTime,
                workshop.EndDateTime,
                workshop.Capacity,
                availableSlots
            ));
        }

        return result;
    }
}
