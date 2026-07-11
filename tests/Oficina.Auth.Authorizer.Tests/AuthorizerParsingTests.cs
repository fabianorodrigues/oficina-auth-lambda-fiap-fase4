using System.Reflection;

namespace Oficina.Auth.Authorizer.Tests;

public sealed class AuthorizerParsingTests
{
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

