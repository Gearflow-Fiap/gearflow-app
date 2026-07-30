namespace Customers.Domain.Common;

/// <summary>
/// Validação de CPF/CNPJ preservada do GearFlow original (<c>GearFlow.Domain.Utils</c>). Mantida
/// interna ao Customers BC — é o dono do documento do cliente e da regra que a Lambda de CPF reusa.
/// </summary>
internal static class DocumentValidation
{
    private const int CnpjLength = 14;
    private const int CpfLength = 11;

    public static string OnlyNumbers(string src) =>
        string.IsNullOrEmpty(src) ? src : new string(src.Where(char.IsDigit).ToArray());

    public static bool IsValidCpf(string? src)
    {
        if (string.IsNullOrEmpty(src)) return false;
        src = OnlyNumbers(src);
        if (src.Length != CpfLength) return false;
        if (new string(src[0], CpfLength) == src) return false;

        int[] mult1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

        var temp = src[..9];
        var sum = 0;
        for (var i = 0; i < 9; i++) sum += (temp[i] - '0') * mult1[i];

        var digit = GetDigit(sum);
        temp += digit;

        sum = 0;
        for (var i = 0; i < 10; i++) sum += (temp[i] - '0') * mult2[i];
        digit += GetDigit(sum);

        return src.EndsWith(digit, StringComparison.Ordinal);
    }

    public static bool IsValidCnpj(string? src)
    {
        if (string.IsNullOrEmpty(src)) return false;
        src = OnlyNumbers(src);
        if (src.Length != CnpjLength) return false;

        int[] mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var temp = src[..12];
        var sum = 0;
        for (var i = 0; i < 12; i++) sum += (temp[i] - '0') * mult1[i];

        var digit = GetDigit(sum);
        temp += digit;

        sum = 0;
        for (var i = 0; i < 13; i++) sum += (temp[i] - '0') * mult2[i];
        digit += GetDigit(sum);

        return src.EndsWith(digit, StringComparison.Ordinal);
    }

    private static string GetDigit(int sum)
    {
        var rest = sum % 11;
        rest = rest < 2 ? 0 : 11 - rest;
        return rest.ToString();
    }
}
