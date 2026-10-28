using SkillHub.Application.Dtos.Excel;

namespace SkillHub.Application.Interfaces.External;

public interface IExcelService
{
    Task<ImportResultDto> ImportEnrollmentsAsync(Stream fileStream, string staffId);
}
