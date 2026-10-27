using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.External;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using ApplicationException = SkillHub.Application.Common.Exceptions.ApplicationException;

namespace SkillHub.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfService? _pdfService;
    private readonly IUserService? _userService;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IUnitOfWork unitOfWork,
        IPdfService? pdfService = null,
        IUserService? userService = null)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _unitOfWork = unitOfWork;
        _pdfService = pdfService;
        _userService = userService;
    }

    public Task<EnrollmentDto> EnrollAsync(Guid workshopId, string participantId, CancellationToken ct = default)
    {
        return EnrollAsync(participantId, new CreateEnrollmentRequestDto(workshopId), ct);
    }

    public async Task<EnrollmentDto> EnrollAsync(string participantId, CreateEnrollmentRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(participantId))
        {
            throw new ApplicationException("El identificador del participante es obligatorio.");
        }

        var workshop = await _workshopRepository.GetByIdAsync(request.WorkshopId, ct);
        if (workshop is null)
        {
            throw new ApplicationException($"No se encontró el taller con ID '{request.WorkshopId}'.");
        }

        if (workshop.StartDateTime <= DateTime.UtcNow)
        {
            throw new ApplicationException("No es posible inscribirse en un taller que ya ha iniciado.");
        }

        // 1. Validar cupos usando la regla de negocio de la entidad Workshop
        var activeCount = await _enrollmentRepository.GetActiveEnrollmentsCountByWorkshopIdAsync(workshop.Id, ct);
        if (!workshop.HasAvailableCapacity(activeCount))
        {
            throw new ApplicationException("El taller no tiene cupos disponibles.");
        }

        // 2. Evitar inscripciones duplicadas activas
        var isAlreadyEnrolled = await _enrollmentRepository.HasActiveEnrollmentAsync(workshop.Id, participantId, ct);
        if (isAlreadyEnrolled)
        {
            throw new ApplicationException("Ya cuentas con una inscripción activa en este taller.");
        }

        // 3. Evitar cruces de horario con otros talleres activos
        var hasOverlap = await _enrollmentRepository.HasOverlappingActiveEnrollmentAsync(participantId, workshop.StartDateTime, workshop.EndDateTime, ct);
        if (hasOverlap)
        {
            throw new ApplicationException("El horario de este taller se cruza con otra de tus inscripciones activas.");
        }

        // 4. Crear entidad Enrollment
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            WorkshopId = workshop.Id,
            ParticipantId = participantId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        await _enrollmentRepository.AddAsync(enrollment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new EnrollmentDto(
            enrollment.Id,
            workshop.Code,
            workshop.Title,
            workshop.StartDateTime,
            workshop.EndDateTime,
            enrollment.Status,
            enrollment.EnrolledAt
        );
    }

    public async Task CancelAsync(Guid enrollmentId, string currentUserId, bool isStaff, CancellationToken ct = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null)
        {
            throw new ApplicationException($"No se encontró la inscripción con ID '{enrollmentId}'.");
        }

        if (!isStaff && enrollment.ParticipantId != currentUserId)
        {
            throw new ApplicationException("No tienes permisos para cancelar esta inscripción.");
        }

        var workshop = enrollment.Workshop ?? await _workshopRepository.GetByIdAsync(enrollment.WorkshopId, ct);
        if (workshop is not null && workshop.StartDateTime <= DateTime.UtcNow)
        {
            throw new ApplicationException("No se puede cancelar una inscripción para un taller que ya ha comenzado.");
        }

        enrollment.Cancel(currentUserId);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<EnrollmentDto>> GetByParticipantIdAsync(string participantId, CancellationToken ct = default)
    {
        return GetUserEnrollmentsAsync(participantId, ct);
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetUserEnrollmentsAsync(string participantId, CancellationToken ct = default)
    {
        var enrollments = await _enrollmentRepository.GetByParticipantIdAsync(participantId, ct);

        return enrollments.Select(e => new EnrollmentDto(
            e.Id,
            e.Workshop?.Code ?? string.Empty,
            e.Workshop?.Title ?? string.Empty,
            e.Workshop?.StartDateTime ?? DateTime.MinValue,
            e.Workshop?.EndDateTime ?? DateTime.MinValue,
            e.Status,
            e.EnrolledAt
        )).ToList();
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default)
    {
        var enrollments = await _enrollmentRepository.GetByWorkshopIdAsync(workshopId, ct);

        return enrollments.Select(e => new EnrollmentDto(
            e.Id,
            e.Workshop?.Code ?? string.Empty,
            e.Workshop?.Title ?? string.Empty,
            e.Workshop?.StartDateTime ?? DateTime.MinValue,
            e.Workshop?.EndDateTime ?? DateTime.MinValue,
            e.Status,
            e.EnrolledAt
        )).ToList();
    }

    public async Task<byte[]> GetCertificateAsync(Guid enrollmentId, string currentUserId, bool isStaff, CancellationToken ct = default)
    {
        if (_pdfService is null)
        {
            throw new ApplicationException("El servicio de generación de certificados PDF no está configurado.");
        }

        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null)
        {
            throw new ApplicationException($"No se encontró la inscripción con ID '{enrollmentId}'.");
        }

        if (!isStaff && enrollment.ParticipantId != currentUserId)
        {
            throw new ApplicationException("No tienes autorización para descargar este certificado.");
        }

        var participantName = "Participante";
        var participantEmail = "participante@skillhub.com";
        if (_userService is not null)
        {
            participantName = await _userService.GetUserFullNameAsync(enrollment.ParticipantId) ?? participantName;
        }

        var dto = new EnrollmentDto(
            enrollment.Id,
            enrollment.Workshop?.Code ?? "TALLER",
            enrollment.Workshop?.Title ?? "Taller SkillHub",
            enrollment.Workshop?.StartDateTime ?? DateTime.MinValue,
            enrollment.Workshop?.EndDateTime ?? DateTime.MinValue,
            enrollment.Status,
            enrollment.EnrolledAt
        );

        return _pdfService.GenerateEnrollmentCertificate(dto, participantName, participantEmail);
    }
}
