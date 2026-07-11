namespace Oficina.Auth.Shared;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "oficina";
    public string Audience { get; init; } = "oficina-api";
    public int ExpirationMinutes { get; init; } = 60;
    public int ClockSkewSeconds { get; init; } = 60;
    public string SecretName { get; init; } = "/oficina/auth/jwt";
    public string SigningKeyPropertyName { get; init; } = "SigningKey";
}

