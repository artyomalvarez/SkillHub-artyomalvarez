using SkillHub.Application.DTOs.Reports;

namespace SkillHub.Application.Services;

public interface IReportService
{
    Task<IReadOnlyList<TopWorkshopDto>> GetTopWorkshopsAsync(CancellationToken ct = default);
}
