using System.Security.Cryptography;

namespace Oficina.Auth.Shared;

public sealed class Pbkdf2PasswordVerifier : IPasswordVerifier
{
    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(password))
            return false;

        try
        {
            var parts = passwordHash.Split('$');
            if (parts.Length != 4 ||
                !string.Equals(parts[0], "PBKDF2-SHA256", StringComparison.Ordinal) ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations < 100_000)
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

