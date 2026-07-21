namespace Oficina.Auth.Shared;

public sealed record AuthUser(string Id, string Cpf, string Role, string Name, bool Active);

