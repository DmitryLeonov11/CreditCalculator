using System.Net;
using System.Security.Cryptography;
using System.Text;
using CreditCalculator.Application.Abstractions;
using CreditCalculator.Application.Contracts;
using CreditCalculator.Application.Exceptions;
using CreditCalculator.Application.Options;
using CreditCalculator.Domain.Entities;
using CreditCalculator.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CreditCalculator.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IAppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RegisteredUserResponse> RegisterAsync(
        RegisterRequest request,
        Func<string, string> buildConfirmationLink,
        CancellationToken cancellationToken = default)
    {
        var email = User.NormalizeEmail(request.Email);

        if (await _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new BusinessRuleException("Пользователь с таким email уже зарегистрирован.", HttpStatusCode.Conflict);

        var now = _timeProvider.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            Role = Role.Client,
            CreatedAt = now,
            PersonalDataConsentAt = now
        };
        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var confirmationLink = buildConfirmationLink(_emailConfirmationTokenService.CreateToken(user.Id));
        await _emailSender.SendAsync(
            user.Email,
            "Подтверждение регистрации в кредитном калькуляторе",
            $"Чтобы подтвердить email, перейдите по ссылке: {confirmationLink}",
            cancellationToken);

        return new RegisteredUserResponse(user.Id, user.Email);
    }

    public async Task ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var userId = _emailConfirmationTokenService.ReadUserId(token);
        var user = userId is null
            ? null
            : await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
            throw new BusinessRuleException("Ссылка для подтверждения недействительна или устарела.");

        user.EmailConfirmed = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (user is null)
        {
            // Хешируем впустую, чтобы по времени ответа нельзя было отличить несуществующий email от неверного пароля.
            _passwordHasher.Hash(new User(), request.Password);
            throw new AuthenticationFailedException("Неверный email или пароль.");
        }

        if (!_passwordHasher.Verify(user, request.Password))
            throw new AuthenticationFailedException("Неверный email или пароль.");

        if (!user.EmailConfirmed)
            throw new BusinessRuleException("Email не подтверждён. Перейдите по ссылке из письма.", HttpStatusCode.Forbidden);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(request.RefreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.Token == tokenHash, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        if (storedToken is null || !storedToken.IsActive(now))
            throw new AuthenticationFailedException("Refresh-токен недействителен или истёк.");

        // Ротация: старый токен отзываем, чтобы перехваченный токен работал не дольше одного обновления.
        // Отзыв — условный UPDATE: из параллельных запросов с одним токеном выигрывает ровно один.
        var revoked = await _dbContext.RefreshTokens
            .Where(token => token.Id == storedToken.Id && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);

        if (revoked == 0)
            throw new AuthenticationFailedException("Refresh-токен недействителен или истёк.");

        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        await _dbContext.RefreshTokens
            .Where(token => token.UserId == user.Id && (token.RevokedAt != null || token.ExpiresAt <= now))
            .ExecuteDeleteAsync(cancellationToken);

        var accessToken = _accessTokenGenerator.Generate(user);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenLifetimeDays);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Token = HashRefreshToken(refreshToken),
            ExpiresAt = refreshTokenExpiresAt
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshTokenExpiresAt);
    }

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
