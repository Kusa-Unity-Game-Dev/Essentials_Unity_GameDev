using System;
using System.IO;
using System.Security.Cryptography;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// AES-256-CBC payload encryptor. A fresh random IV is generated per encryption and prepended
    /// to the ciphertext (first 16 bytes), so the same plaintext never produces identical output.
    ///
    /// Threat model: this deters casual save editing. It is NOT strong protection — the key is
    /// derived on-device (see <see cref="DefaultKeyProvider"/>), so a determined attacker with the
    /// binary can recover it. Never store secrets (auth tokens, payment data) in save files.
    /// </summary>
    public class AesEncryption : ISaveEncryptor
    {
        public const string AlgorithmName = "aes256-cbc";
        public string Algorithm => AlgorithmName;

        private readonly byte[] m_Key;

        public AesEncryption(ISaveKeyProvider keyProvider)
        {
            if (keyProvider == null) throw new ArgumentNullException(nameof(keyProvider));
            m_Key = keyProvider.DeriveKey(32);   // 32 bytes = AES-256
        }

        // Output layout: [16-byte IV][ciphertext].
        public byte[] Encrypt(byte[] plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = m_Key;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();

            using var output = new MemoryStream();
            output.Write(aes.IV, 0, aes.IV.Length);   // prepend IV so Decrypt can recover it
            using (var cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plaintext, 0, plaintext.Length);
            }
            return output.ToArray();
        }

        // Reads the IV back off the front, then decrypts the remainder.
        public byte[] Decrypt(byte[] ciphertext)
        {
            using var aes = Aes.Create();
            aes.Key = m_Key;

            const int ivLen = 16;
            if (ciphertext.Length < ivLen)
                throw new CryptographicException("Ciphertext too short to contain IV.");

            var iv = new byte[ivLen];
            Array.Copy(ciphertext, 0, iv, 0, ivLen);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var input = new MemoryStream(ciphertext, ivLen, ciphertext.Length - ivLen);
            using var cs = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
            using var output = new MemoryStream();
            cs.CopyTo(output);
            return output.ToArray();
        }
    }
}
