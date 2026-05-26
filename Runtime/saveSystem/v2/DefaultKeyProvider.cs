using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    public class DefaultKeyProvider : ISaveKeyProvider
    {
        private const string DefaultSalt = "bb.framework.essentials.v2.save";
        private const int Iterations = 50000;

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
