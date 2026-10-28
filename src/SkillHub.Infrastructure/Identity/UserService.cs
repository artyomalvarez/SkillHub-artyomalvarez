using Microsoft.AspNetCore.Identity;
using SkillHub.Application.Interfaces.External;

namespace SkillHub.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task<string?> GetUserFullNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.FullName ?? user?.UserName;
    }

    public async Task<string?> GetUserEmailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email;
    }
}
