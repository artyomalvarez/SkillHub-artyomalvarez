using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public class WorkshopRepository : IWorkshopRepository
{
    private readonly ApplicationDbContext _context;

    public WorkshopRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Workshop?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workshops
            .Include(w => w.Enrollments)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Workshop?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _context.Workshops
            .Include(w => w.Enrollments)
            .FirstOrDefaultAsync(w => w.Code == code, ct);
    }

    public async Task<IReadOnlyList<Workshop>> GetUpcomingAsync(CancellationToken ct = default)
    {
        return await _context.Workshops
            .Include(w => w.Enrollments)
            .Where(w => w.StartDateTime > DateTime.UtcNow)
            .OrderBy(w => w.StartDateTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Workshop>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Workshops
            .Include(w => w.Enrollments)
            .ToListAsync(ct);
    }

    public Task<IReadOnlyList<Workshop>> GetUpcomingWorkshopsAsync(CancellationToken ct = default)
    {
        return GetUpcomingAsync(ct);
    }

    public async Task AddAsync(Workshop workshop, CancellationToken ct = default)
    {
        await _context.Workshops.AddAsync(workshop, ct);
    }
}
