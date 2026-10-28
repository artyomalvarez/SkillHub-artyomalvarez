using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Enrollments;
using SkillHub.Application.Dtos.Workshops;
using SkillHub.Web.Services;

namespace SkillHub.Web.Controllers;

public class StaffController : Controller
{
    private readonly IApiClient _apiClient;

    public StaffController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    private bool IsStaffAuthenticated()
    {
        var token = HttpContext.Session.GetString("JwtToken");
        var role = HttpContext.Session.GetString("UserRole");
        return !string.IsNullOrWhiteSpace(token) && role == "Staff";
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsStaffAuthenticated())
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var workshops = await _apiClient.GetAsync<IReadOnlyList<WorkshopDto>>("/api/v1/workshops")
                            ?? new List<WorkshopDto>();
            return View(workshops);
        }
        catch (ApiException ex)
        {
            ViewBag.Error = ex.Message;
            return View(new List<WorkshopDto>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error al cargar los talleres: {ex.Message}";
            return View(new List<WorkshopDto>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsStaffAuthenticated())
        {
            return RedirectToAction("Login", "Account");
        }

        var instructorEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
        var initialDto = new CreateWorkshopRequestDto(
            Code: string.Empty,
            Title: string.Empty,
            Description: string.Empty,
            InstructorEmail: instructorEmail,
            StartDateTime: DateTime.UtcNow.Date.AddDays(1).AddHours(9),
            EndDateTime: DateTime.UtcNow.Date.AddDays(1).AddHours(11),
            Capacity: 20
        );

        return View(initialDto);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkshopRequestDto dto)
    {
        if (!IsStaffAuthenticated())
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var payload = new CreateWorkshopRequestDto(
                dto.Code,
                dto.Title,
                dto.Description,
                dto.InstructorEmail,
                // Forzamos UTC para que PostgreSQL lo acepte
                DateTime.SpecifyKind(dto.StartDateTime, DateTimeKind.Utc),
                DateTime.SpecifyKind(dto.EndDateTime, DateTimeKind.Utc),
                dto.Capacity
            );

            await _apiClient.PostAsync<WorkshopDto>("/api/v1/workshops", payload);
            TempData["Success"] = $"¡Taller '{dto.Title}' ({dto.Code}) publicado exitosamente!";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Enrollments(Guid id)
    {
        if (!IsStaffAuthenticated())
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var workshop = await _apiClient.GetAsync<WorkshopDto>($"/api/v1/workshops/{id}");
            ViewBag.Workshop = workshop;
            ViewBag.WorkshopId = id;

            var enrollments = await _apiClient.GetAsync<IReadOnlyList<EnrollmentDto>>($"/api/v1/workshops/{id}/enrollments")
                              ?? new List<EnrollmentDto>();

            return View(enrollments);
        }
        catch (ApiException ex)
        {
            ViewBag.Error = ex.Message;
            return View(new List<EnrollmentDto>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error al consultar los inscritos: {ex.Message}";
            return View(new List<EnrollmentDto>());
        }
    }
}
