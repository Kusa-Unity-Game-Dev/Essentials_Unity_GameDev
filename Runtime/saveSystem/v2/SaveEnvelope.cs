using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Metadata stored at the top of every save file (the "$envelope" object).
    /// Lets the system reason about a file — its module, version, integrity — before
    /// trusting or parsing the payload underneath it.
    /// </summary>
    public class SaveEnvelopeHeader
    {
        [JsonProperty("format")] public int Format = SaveEnvelope.CurrentEnvelopeFormat;
        [JsonProperty("moduleId")] public string ModuleId;
        [JsonProperty("moduleVersion")] public int ModuleVersion;
        [JsonProperty("savedAtUtc")] public string SavedAtUtc;
        [JsonProperty("engineVersion")] public string EngineVersion;
        [JsonProperty("packageVersion")] public string PackageVersion;
        [JsonProperty("compression")] public string Compression = "none";
        [JsonProperty("encryption")] public string Encryption = "none";
        [JsonProperty("checksum")] public string Checksum;
    }

    /// <summary>
    /// The on-disk file format and the codec that reads/writes it.
    ///
    /// A file is a JSON document: { "$envelope": {header}, "data": {payload-or-base64} }.
    /// On top of that JSON we layer two optional, independent transforms applied in this order
    /// when writing (and reversed when reading):
    ///
    ///   object --(serialize)--> data JSON --(checksum over these bytes)--> [optional AES] --> document JSON --(optional GZip)--> file bytes
    ///
    /// Important ordering rules:
    ///  - The SHA256 checksum is computed over the *plaintext, uncompressed* data payload, so it
    ///    verifies the actual content regardless of compression/encryption settings.
    ///  - Encryption applies to the data payload only, NOT the header — so the header stays readable
    ///    (you can see what module/version a file is without the key).
    ///  - GZip wraps the whole document last; a magic-byte sniff (0x1f 0x8b) detects it on read.
    /// </summary>
    public class SaveEnvelope
    {
        public const int CurrentEnvelopeFormat = 2;
        public const string PackageVersion = "1.5.0";
        public const string EnvelopeKey = "$envelope";
        public const string DataKey = "data";

        private static readonly byte[] GZipMagic = { 0x1f, 0x8b };

        public SaveEnvelopeHeader Header;
        public JToken Data;

        /// <summary>
        /// Serialize a data payload into final file bytes: stamp the header (checksum, timestamp,
        /// versions), optionally encrypt the payload, optionally GZip the whole document.
        /// </summary>
        public static byte[] Pack(
            SaveEnvelopeHeader header,
            JObject data,
            bool compress,
            ISaveEncryptor encryptor,
            ISerializer serializer)
        {
            // Checksum is taken over the canonical bytes of the plaintext payload, before any transform.
            var dataJson = data.ToString(Formatting.None);
            var dataBytes = Encoding.UTF8.GetBytes(dataJson);
            header.Checksum = "sha256:" + Sha256Hex(dataBytes);
            header.SavedAtUtc = DateTime.UtcNow.ToString("o");
            header.EngineVersion = Application.unityVersion;
            header.PackageVersion = PackageVersion;
            header.Format = CurrentEnvelopeFormat;

            // When encrypted, the data field becomes a base64 string; otherwise it's the raw JObject.
            JToken dataField;
            if (encryptor != null)
            {
                var encrypted = encryptor.Encrypt(dataBytes);
                dataField = new JValue(Convert.ToBase64String(encrypted));
                header.Encryption = encryptor.Algorithm;
            }
            else
            {
                dataField = data;
                header.Encryption = "none";
            }
            header.Compression = compress ? "gzip" : "none";

            var doc = new JObject
            {
                [EnvelopeKey] = JObject.FromObject(header),
                [DataKey] = dataField,
            };
            var docText = doc.ToString(Formatting.None);
            var docBytes = Encoding.UTF8.GetBytes(docText);
            return compress ? GZip(docBytes) : docBytes;
        }

        /// <summary>
        /// Reverse <see cref="Pack"/>: un-GZip if needed, parse JSON, read the header, decrypt the
        /// payload if needed, then verify the checksum. Returns false (with a reason in
        /// <paramref name="error"/>) for any problem — corrupt bytes, missing header, bad key,
        /// or checksum mismatch — so the caller can fall back to a backup.
        /// </summary>
        public static bool TryUnpack(
            byte[] bytes,
            ISaveEncryptor encryptor,
            out SaveEnvelope envelope,
            out string error)
        {
            envelope = null;
            error = null;

            if (bytes == null || bytes.Length == 0)
            {
                error = "empty bytes";
                return false;
            }

            byte[] working = LooksGZipped(bytes) ? UnGZip(bytes) : bytes;
            string text;
            try { text = Encoding.UTF8.GetString(working); }
            catch (Exception e) { error = "utf8 decode failed: " + e.Message; return false; }

            JObject doc;
            try { doc = JObject.Parse(text); }
            catch (Exception e) { error = "json parse failed: " + e.Message; return false; }

            if (doc[EnvelopeKey] == null)
            {
                // No header means this is a v1 file or not ours; caller routes to the legacy reader.
                error = "no envelope header (legacy file)";
                return false;
            }

            SaveEnvelopeHeader header;
            try { header = doc[EnvelopeKey].ToObject<SaveEnvelopeHeader>(); }
            catch (Exception e) { error = "header parse failed: " + e.Message; return false; }

            var dataField = doc[DataKey];
            if (dataField == null)
            {
                error = "missing data field";
                return false;
            }

            // Recover the plaintext payload bytes — either by decrypting, or by re-serializing the JObject.
            JObject dataObject;
            byte[] dataBytes;
            if (header.Encryption != null && header.Encryption != "none")
            {
                if (encryptor == null || encryptor.Algorithm != header.Encryption)
                {
                    error = $"encrypted with '{header.Encryption}' but no matching decryptor configured";
                    return false;
                }
                try
                {
                    var b64 = dataField.Value<string>();
                    var encrypted = Convert.FromBase64String(b64);
                    dataBytes = encryptor.Decrypt(encrypted);
                }
                catch (Exception e) { error = "decryption failed: " + e.Message; return false; }

                try { dataObject = JObject.Parse(Encoding.UTF8.GetString(dataBytes)); }
                catch (Exception e) { error = "decrypted payload not json: " + e.Message; return false; }
            }
            else
            {
                if (dataField.Type != JTokenType.Object)
                {
                    error = "data field is not an object";
                    return false;
                }
                dataObject = (JObject)dataField;
                dataBytes = Encoding.UTF8.GetBytes(dataObject.ToString(Formatting.None));
            }

            // Integrity gate: the payload must hash to exactly what the header recorded at save time.
            if (!string.IsNullOrEmpty(header.Checksum))
            {
                var expected = header.Checksum;
                var actual = "sha256:" + Sha256Hex(dataBytes);
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"checksum mismatch: expected {expected}, got {actual}";
                    return false;
                }
            }

            envelope = new SaveEnvelope { Header = header, Data = dataObject };
            return true;
        }

        /// <summary>Cheap check for whether bytes are a v2 file (contains the envelope key), GZip-aware.</summary>
        public static bool LooksLikeV2(byte[] bytes)
        {
            try
            {
                var working = LooksGZipped(bytes) ? UnGZip(bytes) : bytes;
                var text = Encoding.UTF8.GetString(working);
                return text.Contains("\"" + EnvelopeKey + "\"");
            }
            catch { return false; }
        }

        // GZip files begin with the magic bytes 0x1f 0x8b.
        public static bool LooksGZipped(byte[] bytes)
            => bytes != null && bytes.Length >= 2 && bytes[0] == GZipMagic[0] && bytes[1] == GZipMagic[1];

        public static byte[] GZip(byte[] input)
        {
            using var output = new MemoryStream();
            using (var gz = new GZipStream(output, CompressionMode.Compress)) gz.Write(input, 0, input.Length);
            return output.ToArray();
        }

        public static byte[] UnGZip(byte[] input)
        {
            using var inStream = new MemoryStream(input);
            using var gz = new GZipStream(inStream, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gz.CopyTo(output);
            return output.ToArray();
        }

        public static string Sha256Hex(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
