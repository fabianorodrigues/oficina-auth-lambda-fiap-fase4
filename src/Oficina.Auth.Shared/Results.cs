namespace Oficina.Auth.Shared;

public sealed record AuthTokenResult(string AccessToken, string TokenType, int ExpiresIn, DateTimeOffset ExpiresAt, string Jti);

public sealed record JwtValidationResult(bool IsValid, string? FailureCode, IReadOnlyDictionary<string, string> Claims)
{
    public static JwtValidationResult Valid(IReadOnlyDictionary<string, string> claims) => new(true, null, claims);
    public static JwtValidationResult Invalid(string failureCode) => new(false, failureCode, new Dictionary<string, string>());
}

