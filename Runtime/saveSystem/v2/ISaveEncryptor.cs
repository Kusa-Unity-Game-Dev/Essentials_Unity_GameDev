namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Abstraction over payload encryption. <see cref="Algorithm"/> is written into the envelope
    /// header so a file declares how it was encrypted; on load the configured encryptor's
    /// Algorithm must match or the file is rejected. Default impl: <see cref="AesEncryption"/>.
    /// </summary>
    public interface ISaveEncryptor
    {
        /// <summary>Algorithm tag stored in the envelope header (e.g. "aes256-cbc").</summary>
        string Algorithm { get; }
        byte[] Encrypt(byte[] plaintext);
        byte[] Decrypt(byte[] ciphertext);
    }
}
