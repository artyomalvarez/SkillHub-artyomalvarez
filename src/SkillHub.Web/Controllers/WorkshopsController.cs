using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Workshops;
using SkillHub.Web.Services;

namespace SkillHub.Web.Controllers;

public class WorkshopsController : Controller
{
    private readonly IApiClient _apiClient;

    public WorkshopsController(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var workshops = await _apiClient.GetAsync<List<WorkshopDto>>("api/v1/workshops");
            return View(workshops ?? new List<WorkshopDto>());
        }
        catch (ApiException ex)
        {
            ViewBag.Error = ex.Message;
            return View(new List<WorkshopDto>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"No se pudo cargar el catálogo de talleres: {ex.Message}";
            return View(new List<WorkshopDto>());
        }
    }
}
