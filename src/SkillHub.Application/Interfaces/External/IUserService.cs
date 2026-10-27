namespace SkillHub.Application.Interfaces.External;

public interface IUserService
{
    Task<string?> GetUserIdByEmailAsync(string email);
    Task<string?> GetUserFullNameAsync(string userId);
}
