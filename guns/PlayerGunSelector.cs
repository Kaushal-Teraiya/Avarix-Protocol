using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerGunSelector : NetworkBehaviour
{
    [SerializeField]
    private guns Gun;

    //[SerializeField] private Transform GunParent;
    [SerializeField]
    private List<GunsEffect> Guns;

    [Space]
    [Header("Runtime Filled")]
    [SyncVar]
    public GunsEffect ActiveGun;

    void Awake()
    {
        Debug.Log("Awake function called before Start");

        if (ActiveGun == null)
        {
            Debug.LogError("ActiveGun is NULL in Awake! Possible issue with initialization.");
        }
    }

    void Start()
    {
        Debug.Log("Start function called");

        if (!isLocalPlayer)
            return; // ✅ Ensures only the local player executes this

        NetworkIdentity playerIdentity = GetComponent<NetworkIdentity>();

        GunsEffect gun = Guns.Find(gun => gun.type == Gun);

        if (gun == null)
        {
            Debug.LogError($"No GunScriptableObject found for GunType: {Gun}");
            return;
        }

        if (Guns == null || Guns.Count == 0)
        {
            Debug.LogError("🚨 Guns list is EMPTY on the client!");
            return;
        }

        ActiveGun = gun;

        if (ActiveGun == null)
        {
            Debug.LogError("ActiveGun is still NULL after assignment!");
        }
        else
        {
            Debug.Log("active gun assigned");
        }

        if (playerIdentity == null)
        {
            Debug.LogError("playerIdentity is NULL!");
        }

        if (Guns == null || Guns.Count == 0)
        {
            Debug.LogError("Guns list is EMPTY on the client!");
        }

        StartCoroutine(DelayedSpawn(playerIdentity));
    }

    IEnumerator DelayedSpawn(NetworkIdentity playerIdentity)
    {
        yield return new WaitUntil(() => ActiveGun != null);
        Debug.Log("ActiveGun assigned, now spawning gun...");
        ActiveGun.CmdSpawnGun(playerIdentity);
    }
}
