using System.Reflection;
using System.Text.Json;

namespace Oficina.Auth.Authorizer.Tests;

public sealed class AuthorizerParsingTests
{
    [Fact]
    public void Authorizer_response_serializa_contrato_simple_response()
    {
        var json = JsonSerializer.Serialize(new AuthorizerResponse { IsAuthorized = false });

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("isAuthorized", out _));
        Assert.True(document.RootElement.TryGetProperty("context", out _));
        Assert.False(document.RootElement.TryGetProperty("IsAuthorized", out _));
    }

    [Fact]
    public async Task Token_malformado_retorna_deny_sem_buscar_secret()
    {
        var response = await new Function().FunctionHandler(new HttpApiAuthorizerRequest
        {
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer not-a-real-jwt" }
        }, new TestLambdaContext());

        Assert.False(response.IsAuthorized);
        Assert.Empty(response.Context);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic abc")]
    [InlineData("Bearer ")]
    public void Header_invalido_retorna_nulo(string? header)
    {
        var headers = header is null ? null : new Dictionary<string, string> { ["Authorization"] = header };
        Assert.Null(Extract(headers));
    }

    [Fact]
    public void Header_bearer_extrai_token()
        => Assert.Equal("abc.def", Extract(new Dictionary<string, string> { ["authorization"] = "Bearer abc.def" }));

    private static string? Extract(IDictionary<string, string>? headers)
    {
        var method = typeof(Function).GetMethod("ExtractBearerToken", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string?)method.Invoke(null, [headers]);
    }
}

