using Microsoft.Extensions.Logging;
using Oficina.Auth.Shared;

namespace Oficina.Auth.Cpf;

public sealed class AuthService(
    IAuthUserRepository repository,
    IPasswordVerifier passwordVerifier,
    IJwtTokenService jwtTokenService,
    ILogger<AuthService> logger)
{
    public async Task<AuthResponse?> AuthenticateAsync(string cpf, string password, string correlationId, CancellationToken cancellationToken)
    {
        var user = await repository.FindByCpfAsync(cpf, cancellationToken);
        if (user is null || !user.Ativo || !passwordVerifier.Verify(user.SenhaHash, password))
        {
            logger.LogWarning("AuthenticationFailed CorrelationId={CorrelationId} Cpf={Cpf}", correlationId, Oficina.Auth.Shared.Cpf.Mask(cpf));
            return null;
        }

        var authUser = new AuthUser(user.Id, user.Cpf, user.Role, user.Nome, user.Ativo);
        var token = jwtTokenService.Generate(authUser);
        logger.LogInformation("AuthenticationSucceeded CorrelationId={CorrelationId} Cpf={Cpf} Role={Role}", correlationId, Oficina.Auth.Shared.Cpf.Mask(cpf), user.Role);
        logger.LogInformation("TokenGenerated CorrelationId={CorrelationId} Jti={Jti}", correlationId, token.Jti);
        return new AuthResponse(token.AccessToken, token.TokenType, token.ExpiresIn, new AuthUserResponse(user.Nome, user.Role));
    }
}
