using SkillHub.Application.Dtos.Reports;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Enums;

namespace SkillHub.Application.Services;

public class ReportService : IReportService
{
    private readonly IWorkshopRepository _workshopRepository;

    public ReportService(IWorkshopRepository workshopRepository)
    {
        _workshopRepository = workshopRepository;
    }

    public async Task<IReadOnlyList<TopWorkshopDto>> GetTopWorkshopsAsync(CancellationToken ct = default)
    {
        var workshops = await _workshopRepository.GetAllAsync(ct);

        return workshops
            .Select(w => new TopWorkshopDto(
                w.Code,
                w.Title,
                w.Enrollments.Count(e => e.Status == EnrollmentStatus.Active)
            ))
            .OrderByDescending(r => r.EnrollmentCount)
            .Take(10)
            .ToList();
    }
}
