using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillHub.Application.Dtos.Enrollments;
using SkillHub.Application.Interfaces.External;

namespace SkillHub.Infrastructure.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
        QuestPDF.Settings.UseSystemFonts = true;
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
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(12));

                page.Header()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item().Text("SKILLHUB LEARNING PLATFORM")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Indigo.Medium);
                        col.Item().Text("Constancia de inscripción")
                            .FontSize(18)
                            .SemiBold()
                            .FontColor(Colors.Grey.Darken3);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().AlignCenter().Text("Se hace constar que")
                            .FontSize(14)
                            .Italic();

                        col.Item().AlignCenter().Text(participantName)
                            .FontSize(22)
                            .Bold()
                            .Underline()
                            .FontColor(Colors.Indigo.Darken2);

                        col.Item().AlignCenter().Text($"Correo electrónico: {participantEmail}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().AlignCenter().Text("se encuentra inscrito formalmente en el taller:")
                            .FontSize(13);

                        col.Item().AlignCenter().Text($"\"{enrollment.WorkshopTitle}\" (Código: {enrollment.WorkshopCode})")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        if (!string.IsNullOrWhiteSpace(enrollment.InstructorName))
                        {
                            col.Item().AlignCenter().Text($"Instructor a cargo: {enrollment.InstructorName}")
                                .FontSize(13)
                                .SemiBold()
                                .FontColor(Colors.Grey.Darken3);
                        }

                        col.Item().AlignCenter().Text(
                            $"Fecha y horario: Del {enrollment.StartDateTime:dd/MM/yyyy HH:mm} al {enrollment.EndDateTime:dd/MM/yyyy HH:mm} (UTC)"
                        ).FontSize(12);

                        col.Item().AlignCenter().Text(
                            $"Fecha de inscripción: {enrollment.EnrolledAt:dd/MM/yyyy HH:mm:ss} UTC"
                        ).FontSize(11).FontColor(Colors.Grey.Darken2);

                        col.Item().AlignCenter().Text($"Estado de la inscripción: {enrollment.Status}")
                            .FontSize(11)
                            .FontColor(Colors.Green.Darken2)
                            .Bold();
                    });

                page.Footer()
                    .AlignBottom()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Código de Validación: {enrollment.Id}").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"Fecha de generación del documento: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
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
