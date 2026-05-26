using System.Collections.Generic;
using BB.Framework;            // SaveDataModule lives here
using BB.Framework.SaveV2;     // [SaveModule], JObject migration
using Newtonsoft.Json.Linq;

namespace BB.Framework.SaveV2.Demo
{
    // ---------------------------------------------------------------------------
    // EXAMPLE SAVE MODULE
    //
    // A save module is any class that:
    //   1. inherits SaveDataModule, and
    //   2. is tagged with [SaveModule(id, version)].
    //
    // The `id` ("currency") is the on-disk identity. The file becomes "currency.save".
    // NEVER change the id after release — it's how saved files are matched on load.
    //
    // Public fields are serialized by Newtonsoft.Json. Unlike Unity's JsonUtility, this means
    // you can freely use Dictionary<,>, List<>, nullable types, enums, and nested classes.
    // ---------------------------------------------------------------------------
    [SaveModule(id: "currency", version: 2)]
    public class CurrencyModule : SaveDataModule
    {
        public int Coins;
        public int Gems;

        // Added in schema version 2. Old (v1) save files won't have it — see Migrate() below.
        public Dictionary<string, int> SpecialTokens = new();

        // Called when there is no save file yet (new game) or when recovery falls all the way
        // through. Set the starting state for a fresh player here.
        public override void InitializeDefaults()
        {
            Coins = 100;
            Gems = 0;
            SpecialTokens = new Dictionary<string, int>();
        }

        // Called once per version step when loading an older file. The system invokes this
        // repeatedly (Migrate(data, 1), then Migrate(data, 2), ...) until the payload reaches the
        // current version declared in the attribute. Mutate the raw JSON `data` in place.
        public override void Migrate(JObject data, int fromVersion)
        {
            // v1 -> v2: the SpecialTokens dictionary didn't exist. Give it an empty object so
            // deserialization doesn't leave it null.
            if (fromVersion < 2)
            {
                data["SpecialTokens"] = new JObject();
            }
        }

        // Convenience gameplay API (not required by the save system).
        public void AddCoins(int amount) => Coins += amount;
        public void AddGems(int amount) => Gems += amount;
    }
}
