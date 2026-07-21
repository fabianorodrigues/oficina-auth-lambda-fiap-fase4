namespace Oficina.Auth.Cpf;

public sealed record AuthRequest(string? Cpf, string? Password);

public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, AuthUserResponse User);

public sealed record AuthUserResponse(string Name, string Role);

public sealed record ErrorResponse(string Code, string Message, string? CorrelationId = null);

public sealed record CadastroUserRow(string Id, string Cpf, string Nome, string Role, string SenhaHash, bool Ativo);

