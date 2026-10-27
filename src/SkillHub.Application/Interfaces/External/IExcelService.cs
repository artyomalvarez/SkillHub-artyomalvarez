using SkillHub.Application.DTOs.Excel;

namespace SkillHub.Application.Interfaces.External;

public interface IExcelService
{
    Task<ImportResultDto> ImportEnrollmentsAsync(Stream fileStream, string staffId);
}
