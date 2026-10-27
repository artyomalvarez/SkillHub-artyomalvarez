using SkillHub.Application.DTOs.Workshops;

namespace SkillHub.Application.Services;

public interface IWorkshopService
{
    Task<WorkshopDto> CreateAsync(CreateWorkshopRequestDto dto, CancellationToken ct = default);
    Task<WorkshopDto> CreateWorkshopAsync(CreateWorkshopRequestDto dto, CancellationToken ct = default);
    Task<WorkshopDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkshopDto>> GetUpcomingWorkshopsAsync(CancellationToken ct = default);
}
