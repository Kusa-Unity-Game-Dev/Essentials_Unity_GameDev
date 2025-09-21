namespace BB.Framework
{
    /// <summary>
    /// Strongly-typed key helper to avoid stringly-typed errors.
    /// </summary>
    public readonly struct ValueKey<T>
    {
        public readonly string Path;
        public ValueKey(string path) { Path = path; }
        public override string ToString() => Path;
    }

    /*public static class Keys
    {
        // Example “well-known” keys you can reuse across code:
        public static readonly ValueKey<int>       PlayerLevel      = new("player.level");
        public static readonly ValueKey<Wallet>    Wallet           = new("player.wallet");
        public static readonly ValueKey<RewardBag> PendingRewards   = new("pending.rewards");
        public static readonly ValueKey<UnityEngine.Vector3> AimWorldPos = new("ui.aim.worldpos");
    }*/
}