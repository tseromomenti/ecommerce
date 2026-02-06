namespace Ecommerce.ServiceDefaults.Configuration;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public bool AllowAnyOrigin { get; init; }
    public string[] AllowedOrigins { get; init; } = ["http://localhost:4200"];
}
