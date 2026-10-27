using SkillHub.Application.DTOs.Workshops;
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
        // 1. Validar que el instructor exista por email usando IUserService
        var instructorId = await _userService.GetUserIdByEmailAsync(dto.InstructorEmail);
        if (string.IsNullOrWhiteSpace(instructorId))
        {
            throw new ApplicationException($"No se encontró ningún instructor registrado con el correo '{dto.InstructorEmail}'.");
        }

        // 2. Validar que el código no esté repetido
        var existingWorkshop = await _workshopRepository.GetByCodeAsync(dto.Code, ct);
        if (existingWorkshop is not null)
        {
            throw new ApplicationException($"Ya existe un taller con el código '{dto.Code}'.");
        }

        // 3. Validaciones de negocio adicionales sobre fechas y capacidad
        if (dto.StartDateTime >= dto.EndDateTime)
        {
            throw new ApplicationException("La fecha de inicio debe ser anterior a la fecha de finalización.");
        }

        if (dto.StartDateTime <= DateTime.UtcNow)
        {
            throw new ApplicationException("La fecha de inicio debe ser posterior a la fecha actual.");
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
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            Capacity = dto.Capacity
        };

        await _workshopRepository.AddAsync(workshop, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
