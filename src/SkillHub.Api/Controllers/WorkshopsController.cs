using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Services;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;
    private readonly IEnrollmentService _enrollmentService;

    public WorkshopsController(
        IWorkshopService workshopService,
        IEnrollmentService enrollmentService)
    {
        _workshopService = workshopService;
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkshopDto>>> GetUpcomingWorkshops()
    {
        var workshops = await _workshopService.GetUpcomingWorkshopsAsync();
        return Ok(workshops);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkshopDto>> GetById(Guid id)
    {
        var workshop = await _workshopService.GetByIdAsync(id);
        if (workshop is null)
        {
            return NotFound(new { error = $"Taller con ID '{id}' no encontrado." });
        }

        return Ok(workshop);
    }

    [HttpPost]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<WorkshopDto>> Create([FromBody] CreateWorkshopRequestDto dto)
    {
        var created = await _workshopService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetEnrollmentsByWorkshop(Guid id)
    {
        var enrollments = await _enrollmentService.GetByWorkshopIdAsync(id);
        return Ok(enrollments);
    }
}
