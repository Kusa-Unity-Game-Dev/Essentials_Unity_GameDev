namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Supplies the symmetric key bytes for <see cref="AesEncryption"/>. Decoupled from the cipher
    /// so projects can control key derivation (device-bound, account-bound, cloud-synced, etc.)
    /// without touching encryption code. Default impl: <see cref="DefaultKeyProvider"/>.
    /// </summary>
    public interface ISaveKeyProvider
    {
        /// <summary>Return exactly <paramref name="byteLength"/> bytes of key material (32 for AES-256).</summary>
        byte[] DeriveKey(int byteLength);
    }
}
