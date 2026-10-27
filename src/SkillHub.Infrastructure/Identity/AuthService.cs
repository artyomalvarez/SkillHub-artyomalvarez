using Microsoft.AspNetCore.Identity;
using SkillHub.Application.DTOs.Auth;
using SkillHub.Application.Interfaces.External;
using ApplicationException = SkillHub.Application.Common.Exceptions.ApplicationException;

namespace SkillHub.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole>? _roleManager;
    private readonly JwtGenerator _jwtGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtGenerator jwtGenerator,
        RoleManager<IdentityRole>? roleManager = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtGenerator = jwtGenerator;
        _roleManager = roleManager;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            throw new ApplicationException($"Ya existe un usuario registrado con el correo '{dto.Email}'.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"No se pudo registrar el usuario: {errors}");
        }

        // Asegurar asignación del rol Participant
        const string participantRole = "Participant";
        if (_roleManager != null && !await _roleManager.RoleExistsAsync(participantRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(participantRole));
        }

        await _userManager.AddToRoleAsync(user, participantRole);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtGenerator.GenerateToken(user, roles);

        return new AuthResponseDto(token);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            throw new ApplicationException("Credenciales inválidas.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            throw new ApplicationException("Credenciales inválidas.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtGenerator.GenerateToken(user, roles);

        return new AuthResponseDto(token);
    }
}
