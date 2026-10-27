using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Staff")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("top-workshops")]
    public async Task<ActionResult<IReadOnlyList<TopWorkshopDto>>> GetTopWorkshops()
    {
        var topWorkshops = await _reportService.GetTopWorkshopsAsync();
        return Ok(topWorkshops);
    }
}
