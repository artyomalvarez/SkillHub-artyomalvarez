using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces.External;

namespace SkillHub.Infrastructure.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateEnrollmentCertificate(EnrollmentDto enrollment, string participantName, string participantEmail)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(12));

                page.Header()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item().Text("SKILLHUB LEARNING PLATFORM")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Indigo.Medium);
                        col.Item().Text("CONSTANCIA DE PARTICIPACIÓN")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken3);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().AlignCenter().Text("Se hace constar que")
                            .FontSize(14)
                            .Italic();

                        col.Item().AlignCenter().Text(participantName)
                            .FontSize(22)
                            .Bold()
                            .Underline()
                            .FontColor(Colors.Indigo.Darken2);

                        col.Item().AlignCenter().Text($"Identificado con el correo: {participantEmail}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().AlignCenter().Text("ha participado exitosamente en el taller formativo:")
                            .FontSize(14);

                        col.Item().AlignCenter().Text($"\"{enrollment.WorkshopTitle}\" ({enrollment.WorkshopCode})")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        col.Item().AlignCenter().Text(
                            $"Impartido el {enrollment.StartDateTime:dd/MM/yyyy HH:mm} hasta {enrollment.EndDateTime:dd/MM/yyyy HH:mm} (UTC)"
                        ).FontSize(12);

                        col.Item().AlignCenter().Text($"Estado de la inscripción: {enrollment.Status}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken2);
                    });

                page.Footer()
                    .AlignBottom()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Código de Validación: {enrollment.Id}").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"Fecha de emisión: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("___________________________").FontSize(10);
                            c.Item().Text("Dirección Académica SkillHub").FontSize(10).Bold();
                        });
                    });
            });
        });

        return document.GeneratePdf();
    }
}
