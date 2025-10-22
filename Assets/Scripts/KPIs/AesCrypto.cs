using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class AesCrypto
{

    private const int SaltSize = 16;           // 128 bits
    private const int KeySizeBytes = 32;       // 256 bits
    private const int IvSize = 16;             // 128 bits 
    private const int Pbkdf2Iterations = 100_000;

    // Cifra el texto con base 64
    public static string EncryptString(string plaintext, string passphrase)
    {
        if (plaintext == null) plaintext = "";

        // generar salt + derivar clave
        byte[] salt = RandomBytes(SaltSize);
        using var keyDerive = new Rfc2898DeriveBytes(passphrase, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        byte[] key = keyDerive.GetBytes(KeySizeBytes);

        // generar Random
        byte[] iv = RandomBytes(IvSize);

        // cifrar
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        using (var crypto = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            var data = Encoding.UTF8.GetBytes(plaintext);
            crypto.Write(data, 0, data.Length);
            crypto.FlushFinalBlock();
        }

        byte[] cipher = ms.ToArray();

        // empaquetar [salt|random|cipher]
        using var outStream = new MemoryStream();
        using (var bw = new BinaryWriter(outStream))
        {
            bw.Write(salt);
            bw.Write(iv);
            bw.Write(cipher);
        }
        return Convert.ToBase64String(outStream.ToArray());
    }

    // Descifrar
    public static string DecryptString(string b64, string passphrase)
    {
        if (string.IsNullOrEmpty(b64)) return "";

        byte[] blob = Convert.FromBase64String(b64);
        using var ms = new MemoryStream(blob);
        using var br = new BinaryReader(ms);

        byte[] salt = br.ReadBytes(SaltSize);
        byte[] iv = br.ReadBytes(IvSize);
        byte[] cipher = br.ReadBytes((int)(ms.Length - ms.Position));

        using var keyDerive = new Rfc2898DeriveBytes(passphrase, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        byte[] key = keyDerive.GetBytes(KeySizeBytes);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;

        using var outMs = new MemoryStream();
        using (var crypto = new CryptoStream(outMs, aes.CreateDecryptor(), CryptoStreamMode.Write))
        {
            crypto.Write(cipher, 0, cipher.Length);
            crypto.FlushFinalBlock();
        }

        return Encoding.UTF8.GetString(outMs.ToArray());
    }

    private static byte[] RandomBytes(int n)
    {
        var b = new byte[n];
        RandomNumberGenerator.Fill(b);
        return b;
    }
}
