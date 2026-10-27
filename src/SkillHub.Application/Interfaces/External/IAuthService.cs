using SkillHub.Application.DTOs.Auth;

namespace SkillHub.Application.Interfaces.External;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
