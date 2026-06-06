namespace Core.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = "your-super-secret-key-that-is-at-least-32-characters-long-for-security";
    public string Issuer { get; set; } = "cleanmap-api";
    public string Audience { get; set; } = "cleanmap-users";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
}
