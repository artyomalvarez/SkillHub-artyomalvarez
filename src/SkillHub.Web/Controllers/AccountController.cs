using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Auth;
using SkillHub.Web.Services;

namespace SkillHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly IApiClient _apiClient;

    public AccountController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JwtToken")))
        {
            if (HttpContext.Session.GetString("UserRole") == "Staff")
            {
                return RedirectToAction("Index", "Staff");
            }
            return RedirectToAction("Index", "Workshops");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var response = await _apiClient.PostAsync<AuthResponseDto>("api/v1/auth/login", dto);
            if (response != null && !string.IsNullOrWhiteSpace(response.Token))
            {
                var role = "Participant";
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(response.Token))
                {
                    var jwtToken = handler.ReadJwtToken(response.Token);
                    var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
                        c.Type == ClaimTypes.Role ||
                        c.Type == "role" ||
                        c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

                    if (roleClaim != null && !string.IsNullOrWhiteSpace(roleClaim.Value))
                    {
                        role = roleClaim.Value;
                    }
                }

                HttpContext.Session.SetString("JwtToken", response.Token);
                HttpContext.Session.SetString("UserEmail", dto.Email);
                HttpContext.Session.SetString("UserRole", role);

                if (role == "Staff")
                {
                    return RedirectToAction("Index", "Staff");
                }

                return RedirectToAction("Index", "Workshops");
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas o token no recibido.");
            return View(dto);
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al autenticar: {ex.Message}");
            return View(dto);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JwtToken")))
        {
            if (HttpContext.Session.GetString("UserRole") == "Staff")
            {
                return RedirectToAction("Index", "Staff");
            }
            return RedirectToAction("Index", "Workshops");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _apiClient.PostAsync<AuthResponseDto>("api/v1/auth/register", dto);
            TempData["Success"] = "¡Cuenta creada exitosamente! Por favor inicia sesión.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error en el registro: {ex.Message}");
            return View(dto);
        }
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Workshops");
    }
}
