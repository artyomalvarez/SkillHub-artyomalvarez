using SkillHub.Application.DTOs.Enrollments;

namespace SkillHub.Application.Interfaces.External;

public interface IPdfService
{
    byte[] GenerateEnrollmentCertificate(EnrollmentDto enrollment, string participantName, string participantEmail);
}
