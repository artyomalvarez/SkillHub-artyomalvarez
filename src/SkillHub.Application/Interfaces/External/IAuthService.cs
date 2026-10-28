using SkillHub.Application.Dtos.Auth;

namespace SkillHub.Application.Interfaces.External;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
