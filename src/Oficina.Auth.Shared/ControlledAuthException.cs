namespace Oficina.Auth.Shared;

public sealed class ControlledAuthException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

