using CreditCalculator.Application.Services;
using CreditCalculator.Application.Scoring;
using CreditCalculator.Application.Scoring.Rules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CreditCalculator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICreditProductService, CreditProductService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IScoringRule, AgeScoringRule>();

        return services;
    }
}
