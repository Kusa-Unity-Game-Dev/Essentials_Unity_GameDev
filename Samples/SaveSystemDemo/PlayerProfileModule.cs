using System;
using BB.Framework;
using BB.Framework.SaveV2;

namespace BB.Framework.SaveV2.Demo
{
    // A second example module showing nested data and an enum field.
    // `compressed: true` is the default; shown explicitly here for documentation.
    // Set `encrypted: true` to AES-encrypt this module's file on disk.
    [SaveModule(id: "profile", version: 1, encrypted: false, compressed: true)]
    public class PlayerProfileModule : SaveDataModule
    {
        public string DisplayName;
        public int Level;
        public Difficulty Difficulty;
        public DateTime LastPlayedUtc;
        public Stats Stats = new();

        public override void InitializeDefaults()
        {
            DisplayName = "Player";
            Level = 1;
            Difficulty = Difficulty.Normal;
            LastPlayedUtc = DateTime.UtcNow;
            Stats = new Stats();
        }
    }

    public enum Difficulty { Easy, Normal, Hard }

    // Nested classes serialize fine with Newtonsoft — no [Serializable] needed.
    public class Stats
    {
        public int Deaths;
        public int EnemiesDefeated;
        public float MetersTravelled;
    }
}
