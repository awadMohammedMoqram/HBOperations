using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using HBMail.Application.Common.Behaviors;
using HBMail.Application.Common.Reports;
using HBMail.Application.Workflow;

namespace HBMail.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton<MailStateMachine>();

        // Reports infrastructure
        services.AddMemoryCache();
        services.AddSingleton<IReportRateLimiter, ReportRateLimiter>();
        services.AddScoped<IReportAccessPolicy, ReportAccessPolicy>();
        services.AddSingleton<IReportSanitizer, ReportSanitizer>();

        return services;
    }
}

