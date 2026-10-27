using SkillHub.Domain.Entities;

namespace SkillHub.Application.Interfaces.Persistence;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetByParticipantIdAsync(string participantId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default);
    Task<bool> HasActiveEnrollmentAsync(Guid workshopId, string participantId, CancellationToken ct = default);
    Task<bool> HasOverlappingActiveEnrollmentAsync(string participantId, DateTime start, DateTime end, CancellationToken ct = default);
    Task<int> GetActiveEnrollmentsCountByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default);
    Task AddAsync(Enrollment enrollment, CancellationToken ct = default);
}
