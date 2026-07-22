using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.Extensions.Logging.Abstractions;
using Oficina.Auth.Shared;

namespace Oficina.Auth.Cpf.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void Http_response_serializa_contrato_proxy_v2()
    {
        var json = JsonSerializer.Serialize(new HttpApiResponse
        {
            StatusCode = 400,
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = "{}"
        });

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("statusCode", out _));
        Assert.True(document.RootElement.TryGetProperty("headers", out _));
        Assert.True(document.RootElement.TryGetProperty("body", out _));
        Assert.True(document.RootElement.TryGetProperty("isBase64Encoded", out _));
        Assert.False(document.RootElement.TryGetProperty("StatusCode", out _));
    }

    [Fact]
    public async Task Function_cpf_invalido_retorna_400_sem_dependencias_externas()
    {
        var response = await new Function().FunctionHandler(new APIGatewayHttpApiV2ProxyRequest
        {
            Body = "{\"cpf\":\"00000000000\",\"password\":\"invalid-smoke-test\"}",
            Headers = new Dictionary<string, string>()
        }, new TestLambdaContext());

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("invalid_request", response.Body);
    }

    [Fact]
    public async Task Usuario_valido_emite_token_com_claims()
    {
        var repo = new FakeRepo(new CadastroUserRow("id-1", "12345678909", "Ana", "Admin", Hash("Senha!123"), true));
        var service = Create(repo);

        var response = await service.AuthenticateAsync("12345678909", "Senha!123", "corr", CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal("Admin", response.User.Role);
    }

    [Theory]
    [InlineData(false, "Senha!123")]
    [InlineData(true, "errada")]
    public async Task Usuario_inativo_ou_senha_invalida_retorna_generico(bool ativo, string password)
    {
        var repo = new FakeRepo(new CadastroUserRow("id-1", "12345678909", "Ana", "Admin", Hash("Senha!123"), ativo));
        var service = Create(repo);

        Assert.Null(await service.AuthenticateAsync("12345678909", password, "corr", CancellationToken.None));
    }

    [Fact]
    public async Task Usuario_nao_encontrado_retorna_mesmo_resultado_de_senha_invalida()
        => Assert.Null(await Create(new FakeRepo(null)).AuthenticateAsync("12345678909", "Senha!123", "corr", CancellationToken.None));

    private static AuthService Create(IAuthUserRepository repo)
        => new(repo, new Pbkdf2PasswordVerifier(), new JwtTokenService(new JwtOptions(), "Synthetic-Jwt-Signing-Key-At-Least-32-Bytes!", new SystemClock(), new GuidJtiGenerator()), NullLogger<AuthService>.Instance);

    private static string Hash(string password)
    {
        var salt = Convert.FromBase64String("MTIzNDU2Nzg5MDEyMzQ1Ng==");
        var hash = Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32));
        return $"PBKDF2-SHA256$100000${Convert.ToBase64String(salt)}${hash}";
    }

    private sealed class FakeRepo(CadastroUserRow? row) : IAuthUserRepository
    {
        public Task<CadastroUserRow?> FindByCpfAsync(string cpf, CancellationToken cancellationToken) => Task.FromResult(row);
    }
}

