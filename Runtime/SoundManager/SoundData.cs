using UnityEngine;

[System.Serializable]
public class Sound
{
    public string id; // Unique ID for the sound
    public AudioClip clip; // Audio clip to play
    public float volume = 1f; // Volume level
    public bool loop = false; // Should the sound loop
    public bool isFX = false; // Should the sound loop
}

[CreateAssetMenu(fileName = "SoundData", menuName = "kusa/GameAudio/SoundData")]
public class SoundData : ScriptableObject
{
    public Sound[] sounds; // List of all sounds in the game
}
