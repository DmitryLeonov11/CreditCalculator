namespace CreditCalculator.Application.Abstractions;

public interface IEmailConfirmationTokenService
{
    string CreateToken(Guid userId);
    Guid? ReadUserId(string token);
}
