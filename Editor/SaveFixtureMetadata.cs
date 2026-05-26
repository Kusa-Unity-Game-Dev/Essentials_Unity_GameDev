#if UNITY_EDITOR
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BB.Framework
{
    /// <summary>Full = wipe target slot then write every fixture module. Partial = overwrite only the fixture's modules, leave others intact.</summary>
    public enum FixtureInjectMode { Full, Partial }

    /// <summary>Serialized contents of a fixture's "fixture.meta.json" descriptor file.</summary>
    public class SaveFixtureMetadata
    {
        public const string FileName = "fixture.meta.json";

        [JsonProperty("name")] public string Name;
        [JsonProperty("description")] public string Description;
        [JsonProperty("mode")] public string Mode = "full";   // "full" or "partial"
        [JsonProperty("capturedAt")] public string CapturedAt;
        [JsonProperty("slotSource")] public string SlotSource;
        [JsonProperty("packageVersion")] public string PackageVersion;
        [JsonProperty("modulesIncluded")] public List<string> ModulesIncluded = new();

        public FixtureInjectMode GetMode()
            => string.Equals(Mode, "partial", System.StringComparison.OrdinalIgnoreCase) ? FixtureInjectMode.Partial : FixtureInjectMode.Full;

        public void SetMode(FixtureInjectMode m) => Mode = m == FixtureInjectMode.Partial ? "partial" : "full";
    }
}
#endif
