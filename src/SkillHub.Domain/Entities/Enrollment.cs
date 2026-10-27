using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; }
    
    public Guid WorkshopId { get; set; }
    public Workshop Workshop { get; set; } = null!;

    public string ParticipantId { get; set; } = string.Empty;
    
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }

    // Regla de negocio encapsulada
    public void Cancel(string cancelledBy)
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new DomainException("Solo se pueden cancelar inscripciones activas.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelledBy = cancelledBy;
    }
}