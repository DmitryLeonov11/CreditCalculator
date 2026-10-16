namespace CreditCalculator.Api.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Frontend";

    public string[] AllowedOrigins { get; set; } = [];

    public static bool HasValidOrigins(string[]? origins) =>
        origins is { Length: > 0 }
        && origins.All(origin =>
            !string.IsNullOrWhiteSpace(origin)
            && origin == origin.Trim()
            && Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !uri.Host.Contains('*', StringComparison.Ordinal)
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment));
}