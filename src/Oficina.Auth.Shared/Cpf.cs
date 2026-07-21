namespace Oficina.Auth.Shared;

public static class Cpf
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ControlledAuthException("cpf_required", "CPF obrigatorio.");

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || !IsValid(digits))
            throw new ControlledAuthException("cpf_invalid", "CPF invalido.");

        return digits;
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        try
        {
            normalized = Normalize(value ?? string.Empty);
            return true;
        }
        catch (ControlledAuthException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    public static string Mask(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length < 2 ? "***.***.***-**" : $"***.***.***-{digits[^2..]}";
    }

    private static bool IsValid(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
            return false;

        return Digit(cpf, 9) == cpf[9] - '0' && Digit(cpf, 10) == cpf[10] - '0';
    }

    private static int Digit(string cpf, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (cpf[i] - '0') * (length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}

