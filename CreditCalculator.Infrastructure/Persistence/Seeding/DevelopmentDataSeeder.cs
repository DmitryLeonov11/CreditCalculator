using CreditCalculator.Application.Abstractions;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Infrastructure.Persistence.Seeding;

public sealed class DevelopmentDataSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly SeedOptions _options;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        IOptions<SeedOptions> options,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCreditProductsAsync(cancellationToken);
        await SeedEmployeeAsync(cancellationToken);
    }

    private async Task SeedCreditProductsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.CreditProducts.AnyAsync(cancellationToken))
            return;

        _dbContext.CreditProducts.AddRange(
            new CreditProduct
            {
                Id = Guid.CreateVersion7(),
                Name = "Потребительский «На любые цели»",
                Purpose = CreditPurpose.Consumer,
                MinAmount = 500m,
                MaxAmount = 50_000m,
                MinTermMonths = 3,
                MaxTermMonths = 60,
                BaseRate = 16.5m,
                IsActive = true
            },
            new CreditProduct
            {
                Id = Guid.CreateVersion7(),
                Name = "Потребительский «Экспресс»",
                Purpose = CreditPurpose.Consumer,
                MinAmount = 500m,
                MaxAmount = 10_000m,
                MinTermMonths = 3,
                MaxTermMonths = 24,
                BaseRate = 21m,
                IsActive = true
            },
            new CreditProduct
            {
                Id = Guid.CreateVersion7(),
                Name = "Автокредит",
                Purpose = CreditPurpose.Auto,
                MinAmount = 5_000m,
                MaxAmount = 150_000m,
                MinTermMonths = 12,
                MaxTermMonths = 84,
                BaseRate = 13.9m,
                IsActive = true
            },
            new CreditProduct
            {
                Id = Guid.CreateVersion7(),
                Name = "Ипотека на покупку и строительство жилья",
                Purpose = CreditPurpose.Mortgage,
                MinAmount = 20_000m,
                MaxAmount = 300_000m,
                MinTermMonths = 60,
                MaxTermMonths = 240,
                BaseRate = 11.5m,
                IsActive = true
            });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Добавлены начальные кредитные продукты");
    }

    private async Task SeedEmployeeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.EmployeeEmail) || string.IsNullOrWhiteSpace(_options.EmployeePassword))
        {
            _logger.LogWarning("Тестовый сотрудник не создан: в секции {Section} не заданы email и пароль", SeedOptions.SectionName);
            return;
        }

        var email = User.NormalizeEmail(_options.EmployeeEmail);
        if (await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            return;

        var now = _timeProvider.GetUtcNow();
        var employee = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            Role = Role.Employee,
            EmailConfirmed = true,
            CreatedAt = now,
            PersonalDataConsentAt = now
        };
        employee.PasswordHash = _passwordHasher.Hash(employee, _options.EmployeePassword);

        _dbContext.Users.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Создан тестовый сотрудник {Email}", email);
    }
}
