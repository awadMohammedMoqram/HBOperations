using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using HBOperations.Application.Common.Behaviors;
using HBOperations.Application.Workflow;

namespace HBOperations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton<TransactionStateMachine>();

        return services;
    }
}
