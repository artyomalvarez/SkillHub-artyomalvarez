using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Enrollments;
using SkillHub.Application.Dtos.Excel;
using SkillHub.Application.Interfaces.External;
using SkillHub.Application.Services;
using ApplicationException = SkillHub.Application.Common.Exceptions.ApplicationException;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IExcelService _excelService;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        IExcelService excelService)
    {
        _enrollmentService = enrollmentService;
        _excelService = excelService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EnrollmentDto>> Enroll([FromBody] CreateEnrollmentRequestDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Usuario no identificado en el token." });
        }

        var result = await _enrollmentService.EnrollAsync(dto.WorkshopId, userId);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<EnrollmentDto>>> GetMine()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Usuario no identificado en el token." });
        }

        var enrollments = await _enrollmentService.GetByParticipantIdAsync(userId);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Usuario no identificado en el token." });
        }

        var isStaff = User.IsInRole("Staff");
        await _enrollmentService.CancelAsync(id, userId, isStaff);

        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ImportResultDto>> Import([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Debes proporcionar un archivo Excel válido." });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Staff";

        await using var stream = file.OpenReadStream();
        var result = await _excelService.ImportEnrollmentsAsync(stream, userId);

        return Ok(result);
    }

    [HttpGet("{id}/certificate")]
    [HttpGet("{id:guid}/certificate")]
    [Authorize]
    public async Task<IActionResult> GetCertificate(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ApplicationException("La inscripción no existe o el ID es inválido.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Usuario no identificado en el token." });
        }

        var isStaff = User.IsInRole("Staff");
        var pdfBytes = await _enrollmentService.GetCertificateAsync(id, userId, isStaff);

        return File(pdfBytes, "application/pdf", $"constancia-{id}.pdf");
    }
}
