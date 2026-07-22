using System.IdentityModel.Tokens.Jwt;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SecretsManager;
using Microsoft.Extensions.Logging;
using Oficina.Auth.Shared;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Oficina.Auth.Authorizer;

public sealed class Function
{
    private static readonly JwtSecurityTokenHandler JwtReader = new();
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSimpleConsole(o => o.SingleLine = true));
    private static readonly ISystemClock Clock = new SystemClock();
    private static readonly Lazy<ISecretProvider> SecretProvider = new(CreateSecretProvider);

    public async Task<AuthorizerResponse> FunctionHandler(HttpApiAuthorizerRequest request, ILambdaContext context)
    {
        var logger = LoggerFactory.CreateLogger<Function>();
        try
        {
            var token = ExtractBearerToken(request.Headers);
            if (token is null)
            {
                logger.LogWarning("AuthorizerDenied RequestId={RequestId} Reason=missing_or_invalid_header", context.AwsRequestId);
                return Deny();
            }

            if (!JwtReader.CanReadToken(token))
            {
                logger.LogWarning("AuthorizerDenied RequestId={RequestId} Reason=token_malformed", context.AwsRequestId);
                return Deny();
            }

            var options = ReadJwtOptions();
            var signingKey = await SecretProvider.Value.GetRequiredValueAsync(options.SecretName, options.SigningKeyPropertyName, CancellationToken.None);
            var result = new JwtTokenService(options, signingKey, Clock, new GuidJtiGenerator()).Validate(token);

            if (!result.IsValid)
            {
                logger.LogWarning("AuthorizerDenied RequestId={RequestId} Reason={Reason}", context.AwsRequestId, result.FailureCode);
                return Deny();
            }

            logger.LogInformation("AuthorizerAllowed RequestId={RequestId} Sub={Sub} Role={Role} Cpf={Cpf}", context.AwsRequestId, result.Claims["sub"], result.Claims["role"], Cpf.Mask(result.Claims["cpf"]));
            return new AuthorizerResponse
            {
                IsAuthorized = true,
                Context = result.Claims.ToDictionary(k => k.Key, v => v.Value)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthorizerDenied RequestId={RequestId} Reason=unexpected_error", context.AwsRequestId);
            return Deny();
        }
    }

    private static AuthorizerResponse Deny() => new() { IsAuthorized = false };

    private static string? ExtractBearerToken(IDictionary<string, string>? headers)
    {
        if (headers is null)
            return null;

        var match = headers.FirstOrDefault(h => string.Equals(h.Key, "authorization", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(match))
            return null;

        const string prefix = "Bearer ";
        if (!match.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = match[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static JwtOptions ReadJwtOptions() => new()
    {
        Issuer = GetEnvironment("JWT__ISSUER", "oficina"),
        Audience = GetEnvironment("JWT__AUDIENCE", "oficina-api"),
        ExpirationMinutes = GetIntEnvironment("JWT__EXPIRATION_MINUTES", 60),
        ClockSkewSeconds = GetIntEnvironment("JWT__CLOCK_SKEW_SECONDS", 60),
        SecretName = GetEnvironment("JWT__SECRET_NAME", "/oficina/auth/jwt")
    };

    private static string GetEnvironment(string name, string fallback)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int GetIntEnvironment(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;

    private static ISecretProvider CreateSecretProvider() => new AwsSecretsManagerSecretProvider(
        new AmazonSecretsManagerClient(),
        TimeSpan.FromMinutes(GetIntEnvironment("SECRETS__CACHE_TTL_SECONDS", 300) / 60.0),
        Clock,
        LoggerFactory.CreateLogger<AwsSecretsManagerSecretProvider>());
}

public sealed class HttpApiAuthorizerRequest
{
    public string Version { get; init; } = "2.0";
    public string Type { get; init; } = "REQUEST";
    public string RouteArn { get; init; } = string.Empty;
    public IDictionary<string, string>? Headers { get; init; }
}

