using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DevRoadmap.Services;

internal static class CodigoVerificacaoEmail
{
    internal const int QuantidadeDigitos = 6;

    internal static string GerarCodigo()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }

    internal static byte[] CalcularHash(string codigo)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(codigo));
    }

    internal static bool Corresponde(string codigo, byte[] codigoHash)
    {
        ArgumentNullException.ThrowIfNull(codigoHash);
        return CryptographicOperations.FixedTimeEquals(CalcularHash(codigo), codigoHash);
    }
}
