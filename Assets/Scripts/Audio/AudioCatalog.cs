using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioCatalog",
    menuName = "DungeonFront/Audio/Audio Catalog")]
public class AudioCatalog : ScriptableObject
{
    [Serializable]
    public sealed class AudioEntry
    {
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.25f, 3f)]
        public float pitch = 1f;
    }

    [Header("SFX")]
    public AudioEntry uiClick = new AudioEntry();
    public AudioEntry uiDeny = new AudioEntry();
    public AudioEntry coin = new AudioEntry();
    public AudioEntry footstep = new AudioEntry();
    public AudioEntry gameOver = new AudioEntry();
    public AudioEntry machineBreak = new AudioEntry();
    public AudioEntry machineBreaking = new AudioEntry();
    public AudioEntry repair = new AudioEntry();
    public AudioEntry hammerWhoosh = new AudioEntry();
    public AudioEntry metalTap = new AudioEntry();
    public AudioEntry placeMachine = new AudioEntry();
    public AudioEntry pickupMachine = new AudioEntry();
    public AudioEntry phaseStart = new AudioEntry();
    public AudioEntry phaseEnd = new AudioEntry();
    public AudioEntry questAccept = new AudioEntry();
    public AudioEntry zoneUnlock = new AudioEntry();

    [Header("BGM")]
    public AudioEntry prepare = new AudioEntry();
    public AudioEntry production = new AudioEntry();
}
