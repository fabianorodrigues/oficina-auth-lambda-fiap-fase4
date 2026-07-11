namespace Oficina.Auth.Shared;

public interface IJwtTokenService
{
    AuthTokenResult Generate(AuthUser user);
    JwtValidationResult Validate(string token);
}

public interface ISecretProvider
{
    Task<string> GetRequiredValueAsync(string secretName, string propertyName, CancellationToken cancellationToken);
}

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IJtiGenerator
{
    string Create();
}

public interface IPasswordVerifier
{
    bool Verify(string passwordHash, string password);
}

