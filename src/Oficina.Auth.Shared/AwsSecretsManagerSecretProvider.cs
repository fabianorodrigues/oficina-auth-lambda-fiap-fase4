using System.Collections.Concurrent;
using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;

namespace Oficina.Auth.Shared;

public sealed class AwsSecretsManagerSecretProvider(
    IAmazonSecretsManager client,
    TimeSpan ttl,
    ISystemClock clock,
    ILogger<AwsSecretsManagerSecretProvider> logger) : ISecretProvider
{
    private readonly ConcurrentDictionary<string, Lazy<Task<CachedSecret>>> _cache = new(StringComparer.Ordinal);

    public async Task<string> GetRequiredValueAsync(string secretName, string propertyName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretName) || string.IsNullOrWhiteSpace(propertyName))
            throw new ControlledAuthException("secret_config_invalid", "Configuracao de secret invalida.");

        var lazy = _cache.GetOrAdd(secretName, CreateLazy);
        var cached = await lazy.Value.WaitAsync(cancellationToken);
        if (cached.ExpiresAt <= clock.UtcNow)
        {
            _cache.TryRemove(secretName, out _);
            lazy = _cache.GetOrAdd(secretName, CreateLazy);
            cached = await lazy.Value.WaitAsync(cancellationToken);
        }

        if (!cached.Values.TryGetValue(propertyName, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ControlledAuthException("secret_field_missing", $"Campo obrigatorio ausente no secret {secretName}.");

        return value;
    }

    private Lazy<Task<CachedSecret>> CreateLazy(string secretName)
        => new(() => LoadAsync(secretName), LazyThreadSafetyMode.ExecutionAndPublication);

    private async Task<CachedSecret> LoadAsync(string secretName)
    {
        try
        {
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
            if (string.IsNullOrWhiteSpace(response.SecretString))
                throw new ControlledAuthException("secret_empty", $"Secret {secretName} sem SecretString.");

            var values = ParseSecretValues(secretName, response.SecretString);

            return new CachedSecret(values, clock.UtcNow.Add(ttl));
        }
        catch (ControlledAuthException)
        {
            _cache.TryRemove(secretName, out _);
            throw;
        }
        catch (Exception ex)
        {
            _cache.TryRemove(secretName, out _);
            logger.LogError(ex, "SecretLoadFailed SecretName={SecretName}", secretName);
            throw new ControlledAuthException("secret_load_failed", $"Falha ao carregar secret {secretName}.");
        }
    }

    internal static IReadOnlyDictionary<string, string> ParseSecretValues(string secretName, string secretString)
    {
        using var document = JsonDocument.Parse(secretString);
        if (document.RootElement.ValueKind is not JsonValueKind.Object)
            throw new ControlledAuthException("secret_json_invalid", $"Secret {secretName} invalido.");

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText()
            };

            if (value is not null)
                values[property.Name] = value;
        }

        return values;
    }

    private sealed record CachedSecret(IReadOnlyDictionary<string, string> Values, DateTimeOffset ExpiresAt);
}
