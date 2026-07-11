namespace Oficina.Auth.Shared;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class GuidJtiGenerator : IJtiGenerator
{
    public string Create() => Guid.NewGuid().ToString("N");
}

