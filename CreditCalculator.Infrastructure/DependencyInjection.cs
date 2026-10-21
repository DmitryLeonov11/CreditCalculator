using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Options;
using CreditCalculator.Infrastructure.Authentication;
using CreditCalculator.Infrastructure.Cache;
using CreditCalculator.Infrastructure.Email;
using CreditCalculator.Infrastructure.Persistence;
using CreditCalculator.Infrastructure.Persistence.Seeding;
using CreditCalculator.Infrastructure.Security;
using CreditCalculator.Infrastructure.Services;
using CreditCalculator.Infrastructure.Storage;
using Amazon.S3;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

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
        services.AddOptions<NbrbOptions>().BindConfiguration(NbrbOptions.SectionName);
        services.AddOptions<RedisOptions>().BindConfiguration(RedisOptions.SectionName);

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

        // NBRB клиент и сервис курсов валют
        services.AddHttpClient<NbrbClient>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();

        // Redis кэш
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            var config = new ConfigurationOptions
            {
                EndPoints = { $"{options.Host}:{options.Port}" },
                Password = options.Password,
                AbortOnConnectFail = false,
                ConnectTimeout = 5000,
                SyncTimeout = 5000,
            };
            return ConnectionMultiplexer.Connect(config);
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // S3 документное хранилище
        services.AddOptions<S3Options>().BindConfiguration(S3Options.SectionName);
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = true,
            };
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });
        services.AddScoped<IDocumentStorageService, S3DocumentStorageService>();

        // Фоновый сервис периодического обновления курсов
        services.AddHostedService<ExchangeRateUpdateBackgroundService>();

        return services;
    }
}
