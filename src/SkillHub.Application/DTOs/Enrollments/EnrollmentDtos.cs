using SkillHub.Domain.Enums;

namespace SkillHub.Application.DTOs.Enrollments;

public record EnrollmentDto(
    Guid Id,
    string WorkshopCode,
    string WorkshopTitle,
    DateTime StartDateTime,
    DateTime EndDateTime,
    EnrollmentStatus Status,
    DateTime EnrolledAt
);

public record CreateEnrollmentRequestDto(Guid WorkshopId);
