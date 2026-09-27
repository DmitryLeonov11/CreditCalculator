using System.Security.Cryptography;
using CreditCalculator.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace CreditCalculator.Infrastructure.Security;

public sealed class DataProtectionEmailConfirmationTokenService : IEmailConfirmationTokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(1);

    private readonly ITimeLimitedDataProtector _protector;

    public DataProtectionEmailConfirmationTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("EmailConfirmation").ToTimeLimitedDataProtector();
    }

    public string CreateToken(Guid userId) => _protector.Protect(userId.ToString(), TokenLifetime);

    public Guid? ReadUserId(string token)
    {
        try
        {
            return Guid.Parse(_protector.Unprotect(token));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
