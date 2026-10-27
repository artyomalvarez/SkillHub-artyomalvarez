using ClosedXML.Excel;
using SkillHub.Application.DTOs.Excel;
using SkillHub.Application.Interfaces.External;

namespace SkillHub.Infrastructure.Services;

public class ExcelService : IExcelService
{
    public Task<ImportResultDto> ImportEnrollmentsAsync(Stream fileStream, string staffId)
    {
        var errors = new List<ImportErrorDto>();
        int successCount = 0;

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                errors.Add(new ImportErrorDto(0, "El archivo Excel no contiene hojas de cálculo."));
                return Task.FromResult(new ImportResultDto(0, errors));
            }

            var lastRow = worksheet.LastRowUsed();
            if (lastRow is null || lastRow.RowNumber() < 2)
            {
                errors.Add(new ImportErrorDto(0, "El archivo está vacío o solo contiene la fila de encabezados."));
                return Task.FromResult(new ImportResultDto(0, errors));
            }

            // Se asume fila 1 como encabezados: Columna 1 = ParticipantEmail, Columna 2 = WorkshopCode
            for (int rowNumber = 2; rowNumber <= lastRow.RowNumber(); rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                var participantEmail = row.Cell(1).GetString().Trim();
                var workshopCode = row.Cell(2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(participantEmail) && string.IsNullOrWhiteSpace(workshopCode))
                {
                    continue; // Fila vacía al final
                }

                if (string.IsNullOrWhiteSpace(participantEmail))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "El correo del participante es obligatorio en la columna 1."));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(workshopCode))
                {
                    errors.Add(new ImportErrorDto(rowNumber, "El código del taller es obligatorio en la columna 2."));
                    continue;
                }

                // TODO: Integrar lógica de persistencia / registro en lote:
                // 1. Buscar usuario por email (IUserService / UserManager)
                // 2. Buscar taller por código (IWorkshopRepository)
                // 3. Validar cupos, duplicados y solapamientos
                // 4. Crear la inscripción con staffId como creador o auditor
                // 5. Guardar cambios en UnitOfWork

                successCount++;
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ImportErrorDto(0, $"Error procesando el archivo Excel: {ex.Message}"));
        }

        return Task.FromResult(new ImportResultDto(successCount, errors));
    }
}
