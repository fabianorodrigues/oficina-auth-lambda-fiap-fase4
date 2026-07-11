using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SecretsManager;
using Microsoft.Extensions.Logging;
using Oficina.Auth.Shared;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Oficina.Auth.Cpf;

public sealed class Function
{
    private const string DatabaseSecretNameDefault = "/oficina/auth/database";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddSimpleConsole(o => o.SingleLine = true));
    private static readonly ISystemClock Clock = new SystemClock();
    private static readonly ISecretProvider SecretProvider = new AwsSecretsManagerSecretProvider(
        new AmazonSecretsManagerClient(),
        TimeSpan.FromMinutes(GetIntEnvironment("SECRETS__CACHE_TTL_SECONDS", 300) / 60.0),
        Clock,
        LoggerFactory.CreateLogger<AwsSecretsManagerSecretProvider>());

    private static bool _coldStart = true;

    public async Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var logger = LoggerFactory.CreateLogger<Function>();
        var correlationId = GetCorrelationId(request, context);
        var coldStart = _coldStart;
        _coldStart = false;

        try
        {
            var authRequest = ParseRequest(request.Body);
            var cpf = Oficina.Auth.Shared.Cpf.Normalize(authRequest.Cpf ?? string.Empty);
            var password = ValidatePassword(authRequest.Password);

            var jwtOptions = ReadJwtOptions();
            var jwtKey = await SecretProvider.GetRequiredValueAsync(jwtOptions.SecretName, jwtOptions.SigningKeyPropertyName, CancellationToken.None);
            var connectionString = await SecretProvider.GetRequiredValueAsync(GetEnvironment("DATABASE__SECRET_NAME", DatabaseSecretNameDefault), "ConnectionString", CancellationToken.None);

            var service = new AuthService(
                new SqlAuthUserRepository(connectionString, LoggerFactory.CreateLogger<SqlAuthUserRepository>()),
                new Pbkdf2PasswordVerifier(),
                new JwtTokenService(jwtOptions, jwtKey, Clock, new GuidJtiGenerator()),
                LoggerFactory.CreateLogger<AuthService>());

            var response = await service.AuthenticateAsync(cpf, password, correlationId, CancellationToken.None);
            logger.LogInformation("AuthCpfCompleted CorrelationId={CorrelationId} RequestId={RequestId} ColdStart={ColdStart} DurationMs={DurationMs}", correlationId, context.AwsRequestId, coldStart, stopwatch.ElapsedMilliseconds);
            return response is null
                ? Json(401, new ErrorResponse("invalid_credentials", "CPF ou senha invalidos."))
                : Json(200, response);
        }
        catch (ControlledAuthException ex) when (ex.Code is "cpf_required" or "cpf_invalid" or "password_required" or "password_too_long" or "body_invalid")
        {
            logger.LogWarning("AuthenticationInputInvalid CorrelationId={CorrelationId} Code={Code}", correlationId, ex.Code);
            return Json(400, new ErrorResponse("invalid_request", "Requisicao invalida."));
        }
        catch (JsonException)
        {
            logger.LogWarning("AuthenticationInputInvalid CorrelationId={CorrelationId} Code=body_invalid", correlationId);
            return Json(400, new ErrorResponse("invalid_request", "Requisicao invalida."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AuthCpfFailed CorrelationId={CorrelationId}", correlationId);
            return Json(500, new ErrorResponse("internal_error", "Falha interna.", correlationId));
        }
    }

    private static AuthRequest ParseRequest(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ControlledAuthException("body_invalid", "Body JSON obrigatorio.");

        return JsonSerializer.Deserialize<AuthRequest>(body, JsonOptions)
            ?? throw new ControlledAuthException("body_invalid", "Body JSON invalido.");
    }

    private static string ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ControlledAuthException("password_required", "Password obrigatorio.");

        if (password.Length > 256)
            throw new ControlledAuthException("password_too_long", "Password excede tamanho maximo.");

        return password;
    }

    private static JwtOptions ReadJwtOptions() => new()
    {
        Issuer = GetEnvironment("JWT__ISSUER", "oficina"),
        Audience = GetEnvironment("JWT__AUDIENCE", "oficina-api"),
        ExpirationMinutes = GetIntEnvironment("JWT__EXPIRATION_MINUTES", 60),
        ClockSkewSeconds = GetIntEnvironment("JWT__CLOCK_SKEW_SECONDS", 60),
        SecretName = GetEnvironment("JWT__SECRET_NAME", "/oficina/auth/jwt")
    };

    private static APIGatewayHttpApiV2ProxyResponse Json(int statusCode, object value) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
        Body = JsonSerializer.Serialize(value, JsonOptions)
    };

    private static string GetCorrelationId(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        if (request.Headers is not null &&
            request.Headers.TryGetValue("x-correlation-id", out var value) &&
            !string.IsNullOrWhiteSpace(value))
            return value;

        return context.AwsRequestId ?? Guid.NewGuid().ToString("N");
    }

    private static string GetEnvironment(string name, string fallback)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    private static int GetIntEnvironment(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
}
