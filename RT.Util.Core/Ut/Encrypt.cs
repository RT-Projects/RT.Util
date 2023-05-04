using System;
using System.IO;
using System.Security.Cryptography;
using RT.Util.ExtensionMethods;

namespace RT.Util;

partial class Ut
{
    /// <summary>
    ///     Encrypts bytes using AES in CBC mode.</summary>
    /// <param name="plain">
    ///     Plaintext to encrypt.</param>
    /// <param name="key">
    ///     An AES key to use for encryption. Must be exactly 32 bytes long.</param>
    /// <returns>
    ///     The encrypted data.</returns>
    public static byte[] AesEncrypt(byte[] plain, byte[] key)
    {
        if (plain == null) throw new ArgumentNullException(nameof(plain));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        byte[] iv = RndCrypto.NextBytes(16);
        var encryptor = aes.CreateEncryptor(key, iv);

        using var memoryStream = new MemoryStream();
        memoryStream.Write(iv);
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

        cryptoStream.Write(plain, 0, plain.Length);
        cryptoStream.FlushFinalBlock();
        cryptoStream.Close();

        return memoryStream.ToArray();
    }

    /// <summary>
    ///     Decrypts data encrypted with <see cref="AesEncrypt"/>.</summary>
    /// <param name="cipher">
    ///     The output of <see cref="AesEncrypt"/>.</param>
    /// <param name="key">
    ///     The AES key used to encrypt the data.</param>
    /// <returns>
    ///     The decrypted data.</returns>
    public static byte[] AesDecrypt(byte[] cipher, byte[] key)
    {
        if (cipher == null) throw new ArgumentNullException(nameof(cipher));
        if (key == null) throw new ArgumentNullException(nameof(key));

        var memoryStream = new MemoryStream(cipher);
        byte[] iv = memoryStream.Read(16);

        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        var decryptor = aes.CreateDecryptor(key, iv);

        var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);

        var plainBytes = new byte[cipher.Length];
        int plainByteCount = cryptoStream.Read(plainBytes, 0, plainBytes.Length);
        cryptoStream.Close();

        return plainBytes.Subarray(0, plainByteCount);
    }
}
