using Microsoft.EntityFrameworkCore;
using SkillHub.Application.Interfaces.Persistence;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Infrastructure.Persistence;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _context;

    public EnrollmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByParticipantIdAsync(string participantId, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .Where(e => e.WorkshopId == workshopId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveEnrollmentAsync(Guid workshopId, string participantId, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.WorkshopId == workshopId &&
                           e.ParticipantId == participantId &&
                           e.Status == EnrollmentStatus.Active, ct);
    }

    public async Task<bool> HasOverlappingActiveEnrollmentAsync(string participantId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .Include(e => e.Workshop)
            .AnyAsync(e => e.ParticipantId == participantId &&
                           e.Status == EnrollmentStatus.Active &&
                           e.Workshop.StartDateTime < end &&
                           e.Workshop.EndDateTime > start, ct);
    }

    public async Task<int> GetActiveEnrollmentsCountByWorkshopIdAsync(Guid workshopId, CancellationToken ct = default)
    {
        return await _context.Enrollments
            .CountAsync(e => e.WorkshopId == workshopId &&
                             e.Status == EnrollmentStatus.Active, ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        await _context.Enrollments.AddAsync(enrollment, ct);
    }
}
