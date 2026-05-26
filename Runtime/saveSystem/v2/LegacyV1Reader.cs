using System.Text;
using UnityEngine;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Bridges old v1 save files into v2. v1 files were plain (or GZip'd) JSON written by Unity's
    /// JsonUtility, with no "$envelope" wrapper. This reader detects them and re-parses with the
    /// same JsonUtility path so existing player saves survive the upgrade. After a successful read,
    /// the caller re-saves the module in v2 format, so each legacy file is migrated exactly once.
    /// </summary>
    public static class LegacyV1Reader
    {
        /// <summary>True if the bytes look like a v1 file: valid JSON object with no envelope key.</summary>
        public static bool IsLegacy(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;
            try
            {
                string text = SaveEnvelope.LooksGZipped(bytes)
                    ? Encoding.UTF8.GetString(SaveEnvelope.UnGZip(bytes))
                    : Encoding.UTF8.GetString(bytes);
                return !text.Contains("\"" + SaveEnvelope.EnvelopeKey + "\"") && text.TrimStart().StartsWith("{");
            }
            catch { return false; }
        }

        /// <summary>Populate the module from legacy bytes using JsonUtility (matches v1 write behavior).</summary>
        public static bool TryPopulate(byte[] bytes, SaveDataModule target, out string error)
        {
            error = null;
            try
            {
                string text = SaveEnvelope.LooksGZipped(bytes)
                    ? Encoding.UTF8.GetString(SaveEnvelope.UnGZip(bytes))
                    : Encoding.UTF8.GetString(bytes);

                JsonUtility.FromJsonOverwrite(text, target);
                return true;
            }
            catch (System.Exception e)
            {
                error = "legacy v1 parse failed: " + e.Message;
                return false;
            }
        }
    }
}
