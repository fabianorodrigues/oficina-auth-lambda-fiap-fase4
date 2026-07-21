using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Oficina.Auth.Shared;

namespace Oficina.Auth.Shared.Tests;

public sealed class SharedSecurityTests
{
    private const string Key = "Synthetic-Jwt-Signing-Key-At-Least-32-Bytes!";

    [Theory]
    [InlineData("123.456.789-09", "12345678909")]
    [InlineData("12345678909", "12345678909")]
    public void Cpf_normaliza_mascara(string input, string expected)
        => Assert.Equal(expected, Cpf.Normalize(input));

    [Theory]
    [InlineData("")]
    [InlineData("11111111111")]
    [InlineData("123")]
    public void Cpf_rejeita_invalidos(string input)
        => Assert.Throws<ControlledAuthException>(() => Cpf.Normalize(input));

    [Fact]
    public void Cpf_mascara_sem_expor_valor_completo()
        => Assert.Equal("***.***.***-09", Cpf.Mask("12345678909"));

    [Fact]
    public void Pbkdf2_valida_hash_do_cadastro()
    {
        var salt = Convert.FromBase64String("MTIzNDU2Nzg5MDEyMzQ1Ng==");
        var hash = Convert.ToBase64String(System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("Senha!123", salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32));
        Assert.True(new Pbkdf2PasswordVerifier().Verify($"PBKDF2-SHA256$100000${Convert.ToBase64String(salt)}${hash}", "Senha!123"));
        Assert.False(new Pbkdf2PasswordVerifier().Verify($"PBKDF2-SHA256$100000${Convert.ToBase64String(salt)}${hash}", "errada"));
    }

    [Fact]
    public void Jwt_emite_claims_e_expira_em_60_minutos()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var service = new JwtTokenService(new JwtOptions(), Key, clock, new FixedJti("jti-1"));

        var token = service.Generate(new AuthUser("id-1", "12345678909", "Admin", "Ana", true));
        var result = service.Validate(token.AccessToken);

        Assert.True(result.IsValid);
        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal("id-1", result.Claims["sub"]);
        Assert.Equal("12345678909", result.Claims["cpf"]);
        Assert.Equal("Admin", result.Claims["role"]);
        Assert.Equal("Ana", result.Claims["name"]);
        Assert.Equal("jti-1", result.Claims["jti"]);
    }

    [Fact]
    public void Jwt_rejeita_assinatura_issuer_audience_e_algoritmo_incorretos()
    {
        var service = new JwtTokenService(new JwtOptions(), Key, new SystemClock(), new GuidJtiGenerator());
        var otherKeyToken = new JwtTokenService(new JwtOptions(), "Another-Synthetic-Key-At-Least-32-Bytes!", new SystemClock(), new GuidJtiGenerator())
            .Generate(new AuthUser("id", "12345678909", "Admin", "Ana", true));

        Assert.False(service.Validate(otherKeyToken.AccessToken).IsValid);
        Assert.False(new JwtTokenService(new JwtOptions { Issuer = "outro" }, Key, new SystemClock(), new GuidJtiGenerator()).Validate(otherKeyToken.AccessToken).IsValid);

        var hs512Key = "Synthetic-Jwt-Signing-Key-At-Least-64-Bytes-For-HS512-Validation!";
        var unsigned = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "oficina",
            audience: "oficina-api",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "id"), new Claim("cpf", "12345678909"), new Claim("role", "Admin"), new Claim("name", "Ana"), new Claim(JwtRegisteredClaimNames.Jti, "jti")],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(hs512Key)), SecurityAlgorithms.HmacSha512)));

        Assert.False(service.Validate(unsigned).IsValid);
    }

    [Fact]
    public void Jwt_rejeita_claim_obrigatoria_ausente()
    {
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "oficina",
            audience: "oficina-api",
            claims: [new Claim("cpf", "12345678909"), new Claim("role", "Admin"), new Claim("name", "Ana"), new Claim(JwtRegisteredClaimNames.Jti, "jti")],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)));

        Assert.False(new JwtTokenService(new JwtOptions(), Key, new SystemClock(), new GuidJtiGenerator()).Validate(token).IsValid);
    }

    [Fact]
    public void Jwt_exige_chave_forte()
        => Assert.Throws<ControlledAuthException>(() => new JwtTokenService(new JwtOptions(), "short", new SystemClock(), new GuidJtiGenerator()));

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FixedJti(string value) : IJtiGenerator
    {
        public string Create() => value;
    }
}
