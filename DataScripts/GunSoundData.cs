using UnityEngine;

[CreateAssetMenu(fileName = "GunSoundData", menuName = "GunData/GunSounds")]
public class GunSoundData : ScriptableObject
{
    [System.Serializable]
    public struct GunSoundEntry
    {
        public string gunName;
        public AudioClip sound;
    }

    public GunSoundEntry[] gunSounds;

    public AudioClip GetSound(string gunName)
    {
        foreach (var entry in gunSounds)
        {
            if (entry.gunName.Equals(gunName, System.StringComparison.OrdinalIgnoreCase))
            {
                return entry.sound;
            }
        }
        return null;
    }
}
