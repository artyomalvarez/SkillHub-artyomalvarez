using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Auth;
using SkillHub.Application.Interfaces.External;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        var response = await _authService.RegisterAsync(dto);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }
}
