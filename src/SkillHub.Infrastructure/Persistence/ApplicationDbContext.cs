using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Entities;
using SkillHub.Infrastructure.Identity;

namespace SkillHub.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Workshop>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.HasIndex(w => w.Code)
                .IsUnique();

            entity.Property(w => w.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(w => w.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(w => w.Description)
                .HasMaxLength(1000);

            entity.Property(w => w.InstructorId)
                .IsRequired();

            entity.Property(w => w.Capacity)
                .IsRequired();

            entity.HasMany(w => w.Enrollments)
                .WithOne(e => e.Workshop)
                .HasForeignKey(e => e.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ParticipantId)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.EnrolledAt)
                .IsRequired();

            entity.Property(e => e.CancelledBy)
                .HasMaxLength(150);

            entity.HasOne(e => e.Workshop)
                .WithMany(w => w.Enrollments)
                .HasForeignKey(e => e.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
