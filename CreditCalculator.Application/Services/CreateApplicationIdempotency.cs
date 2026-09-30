using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CreditCalculator.Application.Contracts;

namespace CreditCalculator.Application.Services;

internal static class CreateApplicationIdempotency
{
    public const int MaxKeyLength = 128;

    public static string ComputeRequestHash(CreateApplicationRequest request)
    {
        var payload = string.Join(
            '\n',
            request.CreditProductId.ToString("N"),
            request.Amount.ToString(CultureInfo.InvariantCulture),
            request.TermMonths.ToString(CultureInfo.InvariantCulture));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
