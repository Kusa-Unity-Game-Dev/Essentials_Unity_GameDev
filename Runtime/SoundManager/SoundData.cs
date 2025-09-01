using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// Scriptable: one entry per sound
[Serializable]
public class Sound
{
    public string id;
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;
    [Range(0f,1f)] public float volume = 1f;
    [Range(-3f,3f)] public float pitchMin = 1f;
    [Range(-3f,3f)] public float pitchMax = 1f;
    [Range(0f,1f)] public float spatialBlend = 0f; // 0=2D, 1=3D

    public bool loop = false;
    public bool isMusic = false;
    public bool isSfx = true;

    [Header("Limits")]
    public int maxInstances = 8;
    public float cooldownSeconds = 0f;

    [Header("3D")]
    public float minDistance = 1f;
    public float maxDistance = 50f;
    public AudioRolloffMode rolloff = AudioRolloffMode.Linear;

    public AudioClip GetRandomClip() =>
        (clips != null && clips.Length > 0) ? clips[UnityEngine.Random.Range(0, clips.Length)] : null;

    public float GetRandomPitch() => (pitchMin == pitchMax) ? pitchMin : UnityEngine.Random.Range(pitchMin, pitchMax);
}

[CreateAssetMenu(fileName = "SoundData", menuName = "kusa/GameAudio/SoundData")]
public class SoundData : ScriptableObject
{
    public Sound[] sounds; // List of all sounds in the game
}
