using System;
using System.Threading.Tasks;
using BB.Framework.SaveV2;
using UnityEngine;

namespace BB.Framework.SaveV2.Demo
{
    // ---------------------------------------------------------------------------
    // RUNTIME DEMO
    //
    // Drop this component on an empty GameObject in an empty scene and press Play.
    // An on-screen panel lets you create a slot, mutate currency/profile, save, and load.
    //
    // Read top-to-bottom: Start() is the entire bootstrap you need in a real game.
    // ---------------------------------------------------------------------------
    public class SaveSystemDemo : MonoBehaviour
    {
        [SerializeField] private string m_SlotName = "DemoSlot";

        private SaveSystem m_Save;
        private CurrencyModule m_Currency;
        private PlayerProfileModule m_Profile;
        private string m_Log = "";

        private async void Start()
        {
            // 1. Get (or spawn) the SaveSystem singleton.
            m_Save = SaveSystem.EnsureInstance();

            // 2. Discover every [SaveModule] in the project via reflection and register them.
            m_Save.AutoRegisterModules();

            // 3. Listen for recovery events (corrupt file repaired, defaults applied, etc.).
            m_Save.OnRecovery += ev => Append($"RECOVERY: {ev.Kind} on '{ev.ModuleId}' — {ev.Detail}");

            // 4. Make sure the slot exists, then load all modules into their live instances.
            if (!await m_Save.Storage.SlotExistsAsync(m_SlotName))
                await m_Save.CreateSlotAsync(m_SlotName);
            await m_Save.LoadAllAsync(m_SlotName);

            // 5. Grab typed references to use during gameplay.
            m_Currency = m_Save.GetModule<CurrencyModule>();
            m_Profile = m_Save.GetModule<PlayerProfileModule>();

            Append("Booted. Loaded slot '" + m_SlotName + "'.");
        }

        private void OnGUI()
        {
            if (m_Currency == null || m_Profile == null)
            {
                GUILayout.Label("Booting…");
                return;
            }

            GUILayout.BeginArea(new Rect(20, 20, 420, 600), GUI.skin.box);
            GUILayout.Label("<b>Save System Demo</b> — slot: " + m_SlotName, RichLabel());

            GUILayout.Space(8);
            GUILayout.Label($"Coins: {m_Currency.Coins}   Gems: {m_Currency.Gems}");
            GUILayout.Label($"Profile: {m_Profile.DisplayName}  Lv{m_Profile.Level}  ({m_Profile.Difficulty})");
            GUILayout.Label($"Deaths: {m_Profile.Stats.Deaths}  Enemies: {m_Profile.Stats.EnemiesDefeated}");

            GUILayout.Space(8);
            GUILayout.Label("Mutate (in memory only):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 Coins")) m_Currency.AddCoins(10);
            if (GUILayout.Button("+1 Gem")) m_Currency.AddGems(1);
            if (GUILayout.Button("+1 Level")) m_Profile.Level++;
            if (GUILayout.Button("+1 Death")) m_Profile.Stats.Deaths++;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Persistence:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save All")) RunAsync(() => m_Save.SaveAllAsync(m_SlotName), "Saved.");
            if (GUILayout.Button("Load All")) RunAsync(ReloadAsync, "Reloaded.");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("Reset slot to defaults"))
            {
                m_Currency.InitializeDefaults();
                m_Profile.InitializeDefaults();
                Append("In-memory state reset to defaults (not yet saved).");
            }

            GUILayout.Space(8);
            GUILayout.Label("Saved files: " + Application.persistentDataPath + "/GameSlots/GS_" + m_SlotName);
            GUILayout.Space(8);
            GUILayout.Label(m_Log);
            GUILayout.EndArea();
        }

        private async Task ReloadAsync()
        {
            await m_Save.LoadAllAsync(m_SlotName);
            m_Currency = m_Save.GetModule<CurrencyModule>();
            m_Profile = m_Save.GetModule<PlayerProfileModule>();
        }

        private async void RunAsync(Func<Task> op, string okMessage)
        {
            try { await op(); Append(okMessage); }
            catch (Exception e) { Append("ERROR: " + e.Message); }
        }

        private void Append(string line)
        {
            m_Log = "• " + line + "\n" + m_Log;
            if (m_Log.Length > 1200) m_Log = m_Log.Substring(0, 1200);
            Debug.Log("[SaveSystemDemo] " + line);
        }

        private static GUIStyle RichLabel()
        {
            return new GUIStyle(GUI.skin.label) { richText = true };
        }
    }
}
