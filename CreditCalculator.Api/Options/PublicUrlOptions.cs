namespace CreditCalculator.Api.Options;

// Адрес, по которому API видят клиенты, — для ссылок в письмах.
public sealed class PublicUrlOptions
{
    public const string SectionName = "PublicUrl";

    public string BaseUrl { get; set; } = string.Empty;
}
