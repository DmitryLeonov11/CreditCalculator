namespace CreditCalculator.Infrastructure.Cache;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6379;
    public string? Password { get; set; }
    public int DefaultExpiryMinutes { get; set; } = 60;
}