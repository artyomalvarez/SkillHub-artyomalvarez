using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Dtos.Enrollments;
using SkillHub.Web.Services;

namespace SkillHub.Web.Controllers
{
    public class EnrollmentsController : Controller
    {
        private readonly IApiClient _apiClient;

        public EnrollmentsController(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Método auxiliar para verificar si hay sesión
        private bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken"));
        }

        public async Task<IActionResult> Mine()
        {
            // Si no hay token, lo mandamos al Login
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var enrollments = await _apiClient.GetAsync<IEnumerable<EnrollmentDto>>("/api/v1/enrollments/mine");
            return View(enrollments);
        }

        [HttpPost]
        public async Task<IActionResult> Enroll(Guid workshopId)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Account");

            try
            {
                await _apiClient.PostAsync<object>("/api/v1/enrollments", new { WorkshopId = workshopId });
                TempData["Success"] = "¡Inscripción realizada exitosamente!";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la inscripción: {ex.Message}";
            }

            return RedirectToAction(nameof(Mine));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Account");

            try
            {
                await _apiClient.PatchAsync<object>($"/api/v1/enrollments/{id}/cancel", new { });
                TempData["Success"] = "Inscripción cancelada correctamente.";
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cancelar la inscripción: {ex.Message}";
            }

            return RedirectToAction(nameof(Mine));
        }

        [HttpGet]
        public async Task<IActionResult> Certificate(Guid id)
        {
            if (!IsAuthenticated())
                return RedirectToAction("Login", "Account");

            try
            {
                var pdfBytes = await _apiClient.GetByteArrayAsync($"/api/v1/enrollments/{id}/certificate");
                return File(pdfBytes, "application/pdf", $"Constancia_{id}.pdf");
            }
            catch (ApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Mine));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al descargar la constancia: {ex.Message}";
                return RedirectToAction(nameof(Mine));
            }
        }
    }
}
