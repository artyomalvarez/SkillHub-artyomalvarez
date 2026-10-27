using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> EnrollAsync(Guid workshopId, string participantId, CancellationToken ct = default);
    Task<EnrollmentDto> EnrollAsync(string participantId, CreateEnrollmentRequestDto request, CancellationToken ct = default);
    Task CancelAsync(Guid enrollmentId, string currentUserId, bool isStaff, CancellationToken ct = default);
    Task<IReadOnlyList<EnrollmentDto>> GetByParticipantIdAsync(string participantId, CancellationToken ct = default);
    Task<IReadOnlyList<EnrollmentDto>> GetUserEnrollmentsAsync(string participantId, CancellationToken ct = default);
    Task<IReadOnlyList<EnrollmentDto>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default);
    Task<byte[]> GetCertificateAsync(Guid enrollmentId, string currentUserId, bool isStaff, CancellationToken ct = default);
}
