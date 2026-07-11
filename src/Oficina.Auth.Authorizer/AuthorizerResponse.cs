using System.Text.Json.Serialization;

namespace Oficina.Auth.Authorizer;

public sealed class AuthorizerResponse
{
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; init; }

    [JsonPropertyName("context")]
    public Dictionary<string, string> Context { get; init; } = [];
}

