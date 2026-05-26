using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BB.Framework.SaveV2
{
    [Serializable]
    public class SlotManifest
    {
        public const string FileName = "manifest.save";
        public const string ModuleId = "__manifest";

        [JsonProperty("slotName")] public string SlotName;
        [JsonProperty("createdUtc")] public string CreatedUtc;
        [JsonProperty("lastSavedUtc")] public string LastSavedUtc;
        [JsonProperty("playtimeSeconds")] public double PlaytimeSeconds;
        [JsonProperty("modules")] public Dictionary<string, int> Modules = new();
        [JsonProperty("screenshotPath")] public string ScreenshotPath;
        [JsonProperty("userLabel")] public string UserLabel;
        [JsonProperty("custom")] public Dictionary<string, string> Custom = new();

        public static SlotManifest CreateNew(string slotName)
        {
            string now = DateTime.UtcNow.ToString("o");
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
