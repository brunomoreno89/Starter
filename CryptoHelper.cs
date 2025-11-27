using System;
using System.Security.Cryptography;
using System.Text;

public static class CryptoHelper
{
    // Gera uma chave de 256 bits (32 bytes) a partir de uma string
    private static byte[] DeriveKey(string key)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    public static string Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = DeriveKey(key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Junta IV + dado criptografado
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptedBase64, string key)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            return encryptedBase64;

        var fullCipher = Convert.FromBase64String(encryptedBase64);

        using var aes = Aes.Create();
        aes.Key = DeriveKey(key);

        var ivLength = aes.BlockSize / 8;
        var iv = new byte[ivLength];
        var cipherBytes = new byte[fullCipher.Length - ivLength];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
        Buffer.BlockCopy(fullCipher, ivLength, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
