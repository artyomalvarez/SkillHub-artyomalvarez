namespace SkillHub.Application.Dtos.Auth;

public record RegisterRequestDto(string FullName, string Email, string Password);

public record LoginRequestDto(string Email, string Password);

public record AuthResponseDto(string Token);
