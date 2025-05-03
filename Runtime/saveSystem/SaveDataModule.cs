using System.IO;
using UnityEngine;
using System.IO.Compression;

public enum SaveFormat { JSON, Binary }

public abstract class SaveDataModule
{
	public static SaveFormat saveFormat = SaveFormat.Binary;

    public abstract string FileName { get; }
    public abstract ESaveModule savemodule { get; }

    // Optional: A hook to initialize default values if no data exists
    public virtual void InitializeDefaults() { }

    // New: Load this module on demand
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
                //var bytes = File.ReadAllBytes(fullPath);
                //JsonUtility.FromJsonOverwrite(System.Text.Encoding.UTF8.GetString(bytes), this);
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
            SaveOnDemand(slotPath);
        }
    }

    // New: Save this module on demand
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
        //File.WriteAllText(fullPath, json);
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

/*
 * how to use the system : 
using System.IO;
using UnityEngine;

[System.Serializable]
public class CurrencySaveModule : SaveDataModule
{
    public override string FileName => "Currency.json";

    public int Coins;
    public int Gems;

    public override void OnSave(string path)
    {
        string fullPath = Path.Combine(path, FileName);
        string json = JsonUtility.ToJson(this);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Currency saved to {fullPath}");
    }

    public override void OnLoad(string path)
    {
        string fullPath = Path.Combine(path, FileName);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            JsonUtility.FromJsonOverwrite(json, this);
            Debug.Log($"Currency loaded from {fullPath}");
        }
        else
        {
            Debug.LogWarning("Currency file not found.");
        }
    }
}

 */