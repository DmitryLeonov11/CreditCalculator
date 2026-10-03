using System.Net;
using System.Text.Json.Serialization;
using CreditCalculator.Api.Authentication;
using CreditCalculator.Api.ErrorHandling;
using CreditCalculator.Api.Options;
using CreditCalculator.Api.RateLimiting;
using CreditCalculator.Api.Validation;
using CreditCalculator.Application;
using CreditCalculator.Application.Scoring;
using CreditCalculator.Infrastructure;
using CreditCalculator.Infrastructure.Persistence;
using CreditCalculator.Infrastructure.Persistence.Seeding;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace CreditCalculator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>())
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                // Лимит попыток входа считается по IP клиента: за прокси без этого все делили бы один лимит.
                // X-Forwarded-For принимаем только от прокси из конфигурации, иначе клиент подменит свой IP.
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
            });
            builder.Services.AddOptions<PublicUrlOptions>()
                .BindConfiguration(PublicUrlOptions.SectionName)
                .Validate(
                    options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                    "Не задан внешний адрес API PublicUrl:BaseUrl (например, https://api.example.com). Укажите его в appsettings или в переменной окружения PublicUrl__BaseUrl.")
                .ValidateOnStart();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();
            builder.Services.AddApplication();
            builder.Services.AddOptions<ScoringOptions>()
                .BindConfiguration(ScoringOptions.SectionName)
                .Validate(
                    options => options.MinimumLivingWageByn > 0
                        && options.BelarusianGoodsPdnThreshold > 0.25m
                        && options.BelarusianGoodsPdnThreshold <= 0.50m,
                    "Scoring:MinimumLivingWageByn должен быть больше нуля, а Scoring:BelarusianGoodsPdnThreshold — больше 0,25 и не больше 0,50.")
                .ValidateOnStart();
            builder.Services.AddInfrastructure();
            builder.Services.AddJwtAuthentication();
            builder.Services.AddAuthRateLimiting();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();

                // В продакшене миграции и начальные данные применяются отдельным шагом деплоя, а не при старте.
                await using var scope = app.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
                await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
