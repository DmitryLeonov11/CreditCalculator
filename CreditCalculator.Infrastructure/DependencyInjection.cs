using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Options;
using CreditCalculator.Infrastructure.Authentication;
using CreditCalculator.Infrastructure.Email;
using CreditCalculator.Infrastructure.Persistence;
using CreditCalculator.Infrastructure.Persistence.Seeding;
using CreditCalculator.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CreditCalculator.Infrastructure;

public static class DependencyInjection
{
    // HMAC-SHA256 требует ключ не короче 256 бит.
    private const int MinSigningKeyLength = 32;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Строка подключения читается при создании контекста, а не при регистрации,
        // чтобы её можно было переопределить из переменных окружения и в интеграционных тестах.
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Не задана строка подключения ConnectionStrings:Default. Укажите её в User Secrets или в переменной окружения ConnectionStrings__Default.");

            options.UseNpgsql(connectionString);
        });
        services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(
                options => options.SigningKey.Length >= MinSigningKeyLength,
                $"Ключ подписи Jwt:SigningKey не задан или короче {MinSigningKeyLength} символов. Укажите его в User Secrets или в переменной окружения Jwt__SigningKey.")
            .ValidateOnStart();
        services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);

        // Ключи в БД, а не в файловой системе контейнера: иначе после пересборки образа или на втором
        // экземпляре API выданные ссылки подтверждения email перестают расшифровываться.
        services.AddDataProtection()
            .SetApplicationName("CreditCalculator")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IEmailConfirmationTokenService, DataProtectionEmailConfirmationTokenService>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
