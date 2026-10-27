namespace SkillHub.Application.DTOs.Workshops;

public record WorkshopDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    string InstructorName,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int Capacity,
    int AvailableSlots
);

public record CreateWorkshopRequestDto(
    string Code,
    string Title,
    string Description,
    string InstructorEmail,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int Capacity
);
