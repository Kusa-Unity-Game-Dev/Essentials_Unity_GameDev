namespace BB.Framework.SaveV2
{
    /// <summary>What kind of recovery/fallback happened during a load.</summary>
    public enum SaveRecoveryKind
    {
        ChecksumMismatch,   // a file failed integrity check; system is moving to a backup
        BackupRestored,     // a .bak.N file loaded successfully after the main file failed
        DefaultsApplied,    // nothing usable found; InitializeDefaults() was called
        LegacyV1Migrated,   // an old pre-envelope file was detected and converted
        SerializerFailed,   // bytes unpacked but couldn't populate the module
        EncryptionMissing,  // file is encrypted but no matching decryptor is configured
    }

    /// <summary>
    /// Raised whenever a load does NOT take the clean happy path. Surfaced two ways by
    /// <see cref="SaveSystem.RaiseRecovery"/>: the <see cref="SaveSystem.OnRecovery"/> C# event,
    /// and the FSM global bus under <see cref="EventName"/>. Games can listen to warn the player
    /// ("save was repaired" / "progress reset") and editor tools log these live.
    /// </summary>
    public class SaveRecoveryEvent
    {
        public const string EventName = "BB.SaveSystem.RecoveryEvent";

        public SaveRecoveryKind Kind;
        public string SlotName;
        public string ModuleId;
        public string FilePath;
        public string Detail;

        public override string ToString()
            => $"[SaveRecovery {Kind}] slot={SlotName} module={ModuleId} path={FilePath} :: {Detail}";
    }
}
