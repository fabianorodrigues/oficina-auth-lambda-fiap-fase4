using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Oficina.Auth.Shared;

public sealed class JwtTokenService(JwtOptions options, string signingKey, ISystemClock clock, IJtiGenerator jtiGenerator) : IJwtTokenService
{
    private readonly SymmetricSecurityKey _securityKey = new(ValidateSigningKey(signingKey));

    public AuthTokenResult Generate(AuthUser user)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(options.ExpirationMinutes);
        var jti = jtiGenerator.Create();
        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new("cpf", user.Cpf),
            new("role", user.Role),
            new(ClaimTypes.Role, user.Role),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Jti, jti)
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AuthTokenResult(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", options.ExpirationMinutes * 60, expires, jti);
    }

    public JwtValidationResult Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return JwtValidationResult.Invalid("token_empty");

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return JwtValidationResult.Invalid("token_malformed");

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                RequireSignedTokens = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds)
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt ||
                !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
                return JwtValidationResult.Invalid("invalid_algorithm");

            var claims = RequiredClaims(principal);
            return claims is null ? JwtValidationResult.Invalid("missing_claim") : JwtValidationResult.Valid(claims);
        }
        catch (SecurityTokenExpiredException)
        {
            return JwtValidationResult.Invalid("token_expired");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return JwtValidationResult.Invalid("invalid_audience");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return JwtValidationResult.Invalid("invalid_issuer");
        }
        catch (SecurityTokenException)
        {
            return JwtValidationResult.Invalid("token_invalid");
        }
        catch (ArgumentException)
        {
            return JwtValidationResult.Invalid("token_invalid");
        }
    }

    private static IReadOnlyDictionary<string, string>? RequiredClaims(ClaimsPrincipal principal)
    {
        var sub = FindClaim(principal, JwtRegisteredClaimNames.Sub) ?? FindClaim(principal, ClaimTypes.NameIdentifier);
        var cpf = FindClaim(principal, "cpf");
        var role = FindClaim(principal, "role") ?? FindClaim(principal, ClaimTypes.Role);
        var name = FindClaim(principal, "name") ?? FindClaim(principal, JwtRegisteredClaimNames.Name);
        var jti = FindClaim(principal, JwtRegisteredClaimNames.Jti);

        if (new[] { sub, cpf, role, name, jti }.Any(value => string.IsNullOrWhiteSpace(value)))
            return null;

        return new Dictionary<string, string>
        {
            ["sub"] = sub!,
            ["cpf"] = cpf!,
            ["role"] = role!,
            ["name"] = name!,
            ["jti"] = jti!
        };
    }

    private static string? FindClaim(ClaimsPrincipal principal, string type)
        => principal.Claims.FirstOrDefault(claim => string.Equals(claim.Type, type, StringComparison.Ordinal))?.Value;

    private static byte[] ValidateSigningKey(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
            throw new ControlledAuthException("jwt_key_missing", "JWT signing key ausente.");

        if (signingKey.Contains('\r') || signingKey.Contains('\n') || signingKey.Contains('\0'))
            throw new ControlledAuthException("jwt_key_invalid", "JWT signing key invalida.");

        if (signingKey.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
            signingKey.Contains("change-me", StringComparison.OrdinalIgnoreCase))
            throw new ControlledAuthException("jwt_key_placeholder", "JWT signing key placeholder.");

        var bytes = Encoding.UTF8.GetBytes(signingKey);
        if (bytes.Length < 32)
            throw new ControlledAuthException("jwt_key_too_short", "JWT signing key deve possuir ao menos 32 bytes.");

        return bytes;
    }
}
