using Microsoft.AspNetCore.Identity;

namespace SkillHub.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
}
