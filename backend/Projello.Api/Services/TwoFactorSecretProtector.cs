using System.Security.Cryptography;
using System.Text;

namespace Projello.Api.Services;

// Encrypts 2FA secrets before they're stored, so a database leak doesn't hand out
// everyone's authenticator codes. AES-GCM with a key from TwoFactor:EncryptionKey
// (falls back to one derived from Jwt:Key if that isn't set).
public static class TwoFactorSecretProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Protect(IConfiguration config, string plainSecret)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plainSecret);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(Key(config), TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Prefix + Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public static string Unprotect(IConfiguration config, string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        // Secrets saved before encryption was added are still plain text
        if (!stored.StartsWith(Prefix)) return stored;

        var data = Convert.FromBase64String(stored[Prefix.Length..]);
        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipher = data.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(Key(config), TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] Key(IConfiguration config)
    {
        var material = config["TwoFactor:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(material)) material = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(material))
            throw new InvalidOperationException("Set TwoFactor:EncryptionKey (or Jwt:Key) to protect 2FA secrets.");

        return HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(material), 32,
            info: Encoding.UTF8.GetBytes("projello-2fa-secret"));
    }
}
