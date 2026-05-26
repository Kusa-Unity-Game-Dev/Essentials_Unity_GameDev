using System;
using System.IO;
using UnityEngine;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace BB.Framework {

public enum SaveFormat { JSON, Binary }

public abstract class SaveDataModule
{
	public static SaveFormat saveFormat = SaveFormat.Binary;

    // Legacy v1 properties. v2 modules with [SaveModule] attribute do NOT need to override these.
    public virtual string FileName => null;
    public virtual ESaveModule savemodule => (ESaveModule)int.MinValue;

    // Optional: A hook to initialize default values if no data exists.
    public virtual void InitializeDefaults() { }

    // v2: Migrate raw JSON payload from older module version to the next version up.
    // Called repeatedly by SaveSystem until module reaches its declared [SaveModule(version: N)].
    // Override per module to mutate `data` in place when adding/removing/renaming fields.
    public virtual void Migrate(JObject data, int fromVersion) { }

    // v1 path: load this module from disk into self.
    [Obsolete("Use BB.Framework.SaveV2.SaveSystem.Instance.LoadAsync(slotName, moduleId) instead.")]
    public void LoadOnDemand(string slotPath)
    {
        string fullPath = Path.Combine(slotPath, FileName);
        if (File.Exists(fullPath))
        {
            Debug.Log($"Module '{savemodule}' loaded on demand from {fullPath}");
			if (saveFormat == SaveFormat.Binary)
            {
				string decompressed = DecompressBytes(File.ReadAllBytes(fullPath));
				JsonUtility.FromJsonOverwrite(decompressed, this);
            }
            else
            {
                string json = File.ReadAllText(fullPath);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }
        else
        {
            Debug.LogWarning($"Module file '{FileName}' not found. Using default values.");
            InitializeDefaults();
#pragma warning disable 0618
            SaveOnDemand(slotPath);
#pragma warning restore 0618
        }
    }

    // v1 path: save this module to disk.
    [Obsolete("Use BB.Framework.SaveV2.SaveSystem.Instance.SaveAsync(slotName, moduleId) instead.")]
    public void SaveOnDemand(string slotPath)
    {
        string fullPath = Path.Combine(slotPath, FileName);
        string json = JsonUtility.ToJson(this, true);
		if (saveFormat == SaveFormat.Binary)
        {
			byte[] compressed = CompressString(json);
			File.WriteAllBytes(fullPath, compressed);
        }
        else
        {
            File.WriteAllText(fullPath, json);
        }
        Debug.Log($"Module '{savemodule}' saved on demand to {fullPath}");
    }

	public static byte[] CompressString(string text)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(text);
		using var output = new MemoryStream();
		using (var gzip = new GZipStream(output, CompressionMode.Compress))
		{
    		gzip.Write(bytes, 0, bytes.Length);
		}
		return output.ToArray();
	}

	public static string DecompressBytes(byte[] data)
	{
		using var input = new MemoryStream(data);
		using var gzip = new GZipStream(input, CompressionMode.Decompress);
		using var reader = new StreamReader(gzip);
		return reader.ReadToEnd();
	}



#if UNITY_EDITOR
	[UnityEditor.MenuItem("Kusa/Toggle Save Format")]
	public static void ToggleSaveFormat()
	{
    	SaveDataModule.saveFormat = SaveDataModule.saveFormat == SaveFormat.JSON
        	? SaveFormat.Binary
        	: SaveFormat.JSON;

    	Debug.Log("Save format set to: " + SaveDataModule.saveFormat);
	}
#endif


}


public enum ESaveModule
{
    ECurrency = 0,
    EGameData,

    E_Slot1 = 100,
    E_Slot2,
    E_Slot3,
    E_Slot4,
    E_Slot5,
    E_Slot6,
    E_Slot7,
    E_Slot8,
    E_Slot9,
    E_Slot10,
    E_Slot11,
    E_Slot12,
    E_Slot13,
    E_Slot14,
    E_Slot15,
    E_Slot16,
    E_Slot17,
    E_Slot18,
    E_Slot19,
    E_Slot20,
}
}
/*
 * v2 usage:
 *
 *   [BB.Framework.SaveV2.SaveModule("currency", version: 1)]
 *   public class CurrencyModule : SaveDataModule
 *   {
 *       public int Coins;
 *       public int Gems;
 *
 *       public override void InitializeDefaults() { Coins = 100; Gems = 50; }
 *   }
 *
 *   // Bootstrap:
 *   var save = BB.Framework.SaveV2.SaveSystem.EnsureInstance();
 *   save.AutoRegisterModules();
 *   await save.CreateSlotAsync("Game1");
 *   await save.LoadAsync("Game1", "currency");
 *   var currency = save.GetModule<CurrencyModule>();
 *   currency.Coins += 10;
 *   await save.SaveAsync("Game1", "currency");
 *
 * v1 (still supported, [Obsolete]):
 *
 *   [System.Serializable]
 *   public class CurrencySaveModule : SaveDataModule
 *   {
 *       public override string FileName => "Currency.json";
 *       public override ESaveModule savemodule => ESaveModule.ECurrency;
 *       public int Coins;
 *   }
 *
 *   SaveSlotManager.Instance.CreateGameSlot("Game1");
 *   SaveManager.Instance.RegisterModule(new CurrencySaveModule());
 *   SaveManager.Instance.LoadModule("Game1", ESaveModule.ECurrency);
 */
