namespace Core.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 secret key. Must be at least 32 characters (256 bits).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "Nexora";

    public string Audience { get; set; } = "NexoraClient";

    /// <summary>
    /// Access token lifetime in minutes. Default 15 minutes.
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token lifetime in days. Default 7 days.
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
