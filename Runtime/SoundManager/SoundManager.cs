using UnityEngine;
using System.Collections.Generic;


public class SoundManager : MonoBehaviour
{
    public class Dict_Sounddata
    {
        public AudioSource m_audioS;
        public Sound m_sound;

        public Dict_Sounddata(AudioSource a_audioS, Sound a_sound)
        {
            m_audioS = a_audioS;
            m_sound = a_sound;
        }
    }

    private static SoundManager s_Instance; // Singleton instance

    [Header("Sound Settings")]
    public SoundData soundData; // Reference to the SoundData ScriptableObject
    public AudioSource audioSourcePrefab; // Prefab for AudioSource
    public Transform audioSourceParent; // Parent for dynamically created audio sources

    private Dictionary<string, Dict_Sounddata> activeAudioSources = new Dictionary<string, Dict_Sounddata>();

    private void Awake()
    {
        if (s_Instance == null)
        {
            s_Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    private void Start()
    {
        //FSM.AddListener_s(GameConstants.E__SOUNDUPDATED, setvolumeForcurrentAudios);
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
        {
            //FSM.RemoveListener_s(GameConstants.E__SOUNDUPDATED, setvolumeForcurrentAudios);
            s_Instance = null;
        }
    }

    public static void PlaySound(string id)
    {
        if (!s_Instance) return;
        Sound sound = s_Instance.GetSoundById(id);
        if (sound == null)
        {
            Debug.LogWarning($"Sound with ID {id} not found!");
            return;
        }

        if (!s_Instance.activeAudioSources.ContainsKey(id))
        {
            // Create a new AudioSource for this sound
            AudioSource newSource = Instantiate(s_Instance.audioSourcePrefab, s_Instance.audioSourceParent);
            s_Instance.ConfigureAudioSource(newSource, sound);
            s_Instance.activeAudioSources[id] = new Dict_Sounddata(newSource, sound);
        }

        s_Instance.PlaySoundNow(sound, id);
    }

    public static void StopSound(string id)
    {
        if (!s_Instance) return;
        if (s_Instance.activeAudioSources.ContainsKey(id))
        {
            s_Instance.activeAudioSources[id].m_audioS.Stop();
        }
    }

    public static void StopAllSounds()
    {
        if (!s_Instance) return;
        foreach (var source in s_Instance.activeAudioSources.Values)
        {
            source.m_audioS.Stop();
        }
    }

    private Sound GetSoundById(string id)
    {
        foreach (var sound in soundData.sounds)
        {
            if (sound.id == id) return sound;
        }
        return null;
    }

    private void ConfigureAudioSource(AudioSource source, Sound sound)
    {
        source.clip = sound.clip;
        source.volume = sound.volume;
        source.loop = sound.loop;
    }

    private void PlaySoundNow(Sound sound, string id)
    {
        /*var gameDataModule = SaveManager.Instance.GetModule<SD_GameCommon>(ESaveModule.EGameData);
        if (gameDataModule != null)
        {
            if (sound.isFX)
                s_Instance.activeAudioSources[id].m_audioS.volume = gameDataModule.FX_allowed ? sound.volume  : 0;
            else
                s_Instance.activeAudioSources[id].m_audioS.volume = gameDataModule.Music_allowed? sound.volume : 0;
        }*/
        s_Instance.activeAudioSources[id].m_audioS.Play();
    }

    private void setvolumeForcurrentAudios(string str)
    {
        /*var gameDataModule = SaveManager.Instance.GetModule<SD_GameCommon>(ESaveModule.EGameData);
        if (gameDataModule != null)
        {
            foreach (Dict_Sounddata dic_data in activeAudioSources.Values)
            {
                if (dic_data.m_sound.isFX)
                    s_Instance.activeAudioSources[dic_data.m_sound.id].m_audioS.volume = gameDataModule.FX_allowed ? dic_data.m_sound.volume : 0;
                else
                    s_Instance.activeAudioSources[dic_data.m_sound.id].m_audioS.volume = gameDataModule.Music_allowed ? dic_data.m_sound.volume : 0;
            }
        }*/

    }
}
