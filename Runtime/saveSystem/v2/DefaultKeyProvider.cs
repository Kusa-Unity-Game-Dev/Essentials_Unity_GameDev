using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Derives the AES key from the device identity using PBKDF2 (50k iterations, SHA256).
    ///
    /// By binding the key to <c>SystemInfo.deviceUniqueIdentifier</c>, saves are only readable on the
    /// machine that wrote them — enough to stop a player editing their own JSON in a text editor.
    /// It is NOT a real secret: anyone with the build can reproduce the derivation. For stronger
    /// needs, implement <see cref="ISaveKeyProvider"/> yourself and assign it to
    /// <see cref="SaveSystem.KeyProvider"/> before any encrypted save/load.
    ///
    /// Note: because the key is device-bound, encrypted saves do NOT transfer between devices.
    /// If you want cloud sync of encrypted saves, supply a stable cross-device key here.
    /// </summary>
    public class DefaultKeyProvider : ISaveKeyProvider
    {
        private const string DefaultSalt = "bb.framework.essentials.v2.save";
        private const int Iterations = 50_000;

        private readonly string m_Passphrase;
        private readonly byte[] m_Salt;

        public DefaultKeyProvider(string passphrase = null, string salt = null)
        {
            m_Passphrase = passphrase ?? SystemInfo.deviceUniqueIdentifier ?? "device-unknown";
            m_Salt = Encoding.UTF8.GetBytes(salt ?? DefaultSalt);
        }

        public byte[] DeriveKey(int byteLength)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(m_Passphrase, m_Salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(byteLength);
        }
    }
}
