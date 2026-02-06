namespace Ecommerce.ServiceDefaults.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ECommerceOrderingSystem";
    public string Audience { get; init; } = "ECommerceOrderingSystem.Client";
    public string SigningKey { get; init; } = "super-secret-dev-signing-key-change-me";
    public int AccessTokenMinutes { get; init; } = 20;
}
