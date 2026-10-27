using Microsoft.Extensions.DependencyInjection;
using SkillHub.Application.Services;

namespace SkillHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IWorkshopService, WorkshopService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
