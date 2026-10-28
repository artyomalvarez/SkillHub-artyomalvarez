namespace SkillHub.Application.Dtos.Enrollments;

public record EnrollmentDto(
    Guid Id,
    string WorkshopCode,
    string WorkshopTitle,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Status,
    DateTime EnrolledAt,
    string InstructorName = "",
    string ParticipantName = "",
    string ParticipantEmail = ""
);

public record CreateEnrollmentRequestDto(Guid WorkshopId);
