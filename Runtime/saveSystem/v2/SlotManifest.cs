using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BB.Framework.SaveV2
{
    /// <summary>
    /// Per-slot metadata stored in its own file ("manifest.save"). Lets a save/load UI show
    /// playtime, last-saved time, a screenshot, and a label WITHOUT loading any gameplay module.
    /// Updated automatically on every SaveAsync/SaveAllAsync.
    /// </summary>
    [Serializable]
    public class SlotManifest
    {
        public const string FileName = "manifest.save";
        public const string ModuleId = "__manifest";   // reserved id; not a real gameplay module

        [JsonProperty("slotName")] public string SlotName;
        [JsonProperty("createdUtc")] public string CreatedUtc;
        [JsonProperty("lastSavedUtc")] public string LastSavedUtc;
        [JsonProperty("playtimeSeconds")] public double PlaytimeSeconds;
        /// <summary>moduleId -> the schema version last written for it in this slot.</summary>
        [JsonProperty("modules")] public Dictionary<string, int> Modules = new();
        [JsonProperty("screenshotPath")] public string ScreenshotPath;
        [JsonProperty("userLabel")] public string UserLabel;
        /// <summary>Free-form key/value for game-specific summary data (chapter id, difficulty, etc.).</summary>
        [JsonProperty("custom")] public Dictionary<string, string> Custom = new();

        public static SlotManifest CreateNew(string slotName)
        {
            var now = DateTime.UtcNow.ToString("o");
            return new SlotManifest
            {
                SlotName = slotName,
                CreatedUtc = now,
                LastSavedUtc = now,
            };
        }

        public void Touch() => LastSavedUtc = DateTime.UtcNow.ToString("o");
    }
}
