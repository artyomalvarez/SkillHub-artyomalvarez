namespace SkillHub.Domain.Entities;

public class Workshop
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public string InstructorId { get; set; } = string.Empty;
    
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int Capacity { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    // Regla: Validar si hay cupo
    public bool HasAvailableCapacity(int activeEnrollmentsCount)
    {
        return activeEnrollmentsCount < Capacity;
    }

    // Regla: Validar cruce de horarios
    public bool OverlapsWith(DateTime start, DateTime end)
    {
        return StartDateTime < end && EndDateTime > start;
    }
}