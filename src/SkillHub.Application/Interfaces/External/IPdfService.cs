using SkillHub.Application.Dtos.Enrollments;

namespace SkillHub.Application.Interfaces.External;

public interface IPdfService
{
    byte[] GenerateEnrollmentCertificate(EnrollmentDto enrollment, string participantName, string participantEmail);
}
