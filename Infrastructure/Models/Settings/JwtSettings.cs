namespace STL.Infrastructure.Models.Settings;

public sealed class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryTime { get; set; } = 3;
    public string SigningKey { get; set; } = string.Empty;
}
