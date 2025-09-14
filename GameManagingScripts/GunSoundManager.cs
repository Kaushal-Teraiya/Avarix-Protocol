using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GunSoundManager : NetworkBehaviour
{
    public static GunSoundManager Instance;

    [SerializeField]
    private playerWeapon currentWeapon;
    private string WeaponName;

    public AudioSource audioSource;
    public Dictionary<string, AudioClip> gunSounds = new Dictionary<string, AudioClip>();

    public AudioClip pistolSound;
    public AudioClip ARsound;
    public AudioClip AquaMGSound;
    public AudioClip GreenSMGSound;
    public AudioClip AK47;
    public AudioClip M4Carbine;
    public AudioClip Sniper;
    public AudioClip MachineGun;
    public AudioClip ShotGun;

    public weaponManager WeaponManager;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (!isLocalPlayer)
            return; // Ensure this script runs only for the local player

        WeaponManager = FindAnyObjectByType<weaponManager>();

        if (WeaponManager == null)
        {
            Debug.LogError("WeaponManager is NULL in Start! Delaying check...");
            Invoke(nameof(DelayedWeaponCheck), 2f);
        }
        else
        {
            WeaponName = WeaponManager.GunNameForSoundFX();
        }

        gunSounds["Pistol"] = pistolSound;
        gunSounds["AR"] = ARsound;
        gunSounds["AquaMG"] = AquaMGSound;
        gunSounds["GreenSMG"] = GreenSMGSound;
        gunSounds["AK47"] = AK47;
        gunSounds["M4Carbine"] = M4Carbine;
        gunSounds["Sniper"] = Sniper;
        gunSounds["MachineGun"] = MachineGun;
        gunSounds["ShotGun"] = ShotGun;

        Debug.Log("GunSounds Dictionary Populated: " + string.Join(", ", gunSounds.Keys));
    }

    void DelayedWeaponCheck()
    {
        WeaponManager = FindAnyObjectByType<weaponManager>();

        if (WeaponManager == null)
        {
            Debug.LogError("WeaponManager is STILL NULL after delay! Make sure it's in the scene.");
            return;
        }

        Debug.Log("WeaponManager found after delay.");
        WeaponName = WeaponManager.GunNameForSoundFX();
    }

    public AudioClip GetGunSound(string gunName)
    {
        if (gunSounds.TryGetValue(gunName, out AudioClip sound))
        {
            return sound;
        }
        else
        {
            Debug.LogError($"Gun sound NOT found for: {gunName}");
            Debug.Log("Available gun names in dictionary: " + string.Join(", ", gunSounds.Keys));
            return null;
        }
    }

    public void PlayShootSound()
    {
        if (!isLocalPlayer)
            return; // Only the local player should call this

        if (WeaponManager == null)
        {
            WeaponManager = FindAnyObjectByType<weaponManager>();
            if (WeaponManager == null)
            {
                Debug.LogError("WeaponManager is STILL NULL in PlayShootSound after reassignment!");
                return;
            }
        }

        string gunName = WeaponManager.GunNameForSoundFX();

        if (string.IsNullOrEmpty(gunName))
        {
            Debug.LogError("GunNameForSoundFX() returned NULL or EMPTY!");
            return;
        }

        AudioClip shootSound = GetGunSound(gunName);

        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound); // Local player hears their own gunfire
            CmdPlayGunSound(gunName); // Tell server to play the sound for all other players
        }
        else
        {
            Debug.LogError($"Shoot sound is null for gun: {gunName}");
        }
    }

    [Command(requiresAuthority = false)] // Runs on the server
    public void CmdPlayGunSound(string gunName)
    {
        // Debug.Log($"🔴 Server received gun sound request for: {gunName}");
        RpcPlayGunSound(gunName);
    }

    [ClientRpc] // Runs on all clients
    void RpcPlayGunSound(string gunName)
    {
        if (gunSounds == null || gunSounds.Count == 0)
        {
            Debug.Log("⚠️ gunSounds dictionary is empty on client, reloading...");
            gunSounds = new Dictionary<string, AudioClip>
            {
                { "Pistol", pistolSound },
                { "AR", ARsound },
                { "GreenSMG", GreenSMGSound },
                { "AK47", AK47 },
                { "M4Carbine", M4Carbine },
                { "Sniper", Sniper },
                { "MachineGun", MachineGun },
                { "ShotGun", ShotGun },
                { "AquaMG", AquaMGSound },
            };
        }

        if (!gunSounds.ContainsKey(gunName))
        {
            Debug.LogError($"❌ Gun sound NOT found for: {gunName}");
            Debug.Log($"Available gun names on this client: {string.Join(", ", gunSounds.Keys)}");
            return;
        }

        AudioClip shootSound = gunSounds[gunName];

        if (shootSound == null)
        {
            Debug.LogError($"❌ AudioClip for {gunName} is NULL!");
            return;
        }

        // Debug.Log($"✅ Playing gun sound '{gunName}' on client");
        // Debug.Log($"🔊 Received RPC to play gun sound: {gunName}");

        if (gunSounds == null || gunSounds.Count == 0)
        {
            Debug.LogError("❌ gunSounds dictionary is EMPTY on this client!");
            return;
        }

        if (!gunSounds.ContainsKey(gunName))
        {
            Debug.LogError($"❌ Gun sound NOT found for: {gunName}");
            Debug.Log($"Available gun names on this client: {string.Join(", ", gunSounds.Keys)}");
            return;
        }

        if (shootSound == null)
        {
            Debug.LogError($"❌ AudioClip for {gunName} is NULL!");
            return;
        }

        audioSource.PlayOneShot(shootSound);
    }
}
