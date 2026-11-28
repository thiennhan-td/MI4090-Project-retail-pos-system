using System.Globalization;
using System.Security.Cryptography;
using Pos.Domain.Interfaces;

namespace Pos.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IMatKhauHasher
{
    private const string Algorithm = "pbkdf2-sha256-v1";
    private const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Bam(string matKhau)
    {
        ArgumentException.ThrowIfNullOrEmpty(matKhau);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return string.Join('$', Algorithm, Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool KiemTra(string matKhau, string matKhauBam)
    {
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrEmpty(matKhauBam))
        {
            return false;
        }

        var parts = matKhauBam.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations is < Iterations or > 2_000_000)
        {
            return false;
        }

        Span<byte> salt = stackalloc byte[SaltSize];
        Span<byte> expected = stackalloc byte[HashSize];
        if (!Convert.TryFromBase64String(parts[2], salt, out var saltLength) || saltLength != SaltSize
            || !Convert.TryFromBase64String(parts[3], expected, out var hashLength) || hashLength != HashSize)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(matKhau, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
