namespace SkillHub.Application.DTOs.Excel;

public record ImportErrorDto(int RowNumber, string Error);

public record ImportResultDto(int SuccessCount, List<ImportErrorDto> Errors);
