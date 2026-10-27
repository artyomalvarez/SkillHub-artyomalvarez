using SkillHub.Domain.Entities;

namespace SkillHub.Application.Interfaces.Persistence;

public interface IWorkshopRepository
{
    Task<Workshop?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Workshop?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Workshop>> GetUpcomingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Workshop>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Workshop workshop, CancellationToken ct = default);
}
