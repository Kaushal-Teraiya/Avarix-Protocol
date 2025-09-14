using UnityEngine;
using Mirror;
using System.Collections;

public class DreyarShield : NetworkBehaviour
{
    public GameObject shieldPrefab; // Assign in Inspector
    public Transform shieldHolder; // Assign ShieldHolder in Inspector
    private GameObject currentShield;

    public float shieldDuration = 5f;

    [SyncVar] private GameObject shieldInstance; // Sync shield across network

    public bool IsShieldActive
    {
        get { return currentShield != null; }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.J) && currentShield == null)
        {
            CmdSpawnShield();
        }
    }

    [Command] // Runs on the server
    void CmdSpawnShield()
    {
        if (shieldInstance != null) return; // Prevent multiple shields

        GameObject shield = Instantiate(shieldPrefab, shieldHolder.position, shieldHolder.rotation);

        // ✅ Ensure it has the "Shield" tag
        shield.tag = "Shield";
        shield.layer = LayerMask.NameToLayer("Shield");

        // ✅ Make sure it has SpawnShieldRipples
        if (!shield.GetComponent<SpawnShieldRipples>())
        {
            shield.AddComponent<SpawnShieldRipples>(); 
        }

        NetworkServer.Spawn(shield, connectionToClient);

        shieldInstance = shield; // SyncVar will update on clients
        RpcAttachShield(shield);

        StartCoroutine(DestroyShieldAfterDuration(shield, shieldDuration));
    }

    [ClientRpc] // Runs on all clients
    void RpcAttachShield(GameObject shield)
    {
        if (shield == null) return;

        // ✅ Set the shield's parent to the player so it moves correctly
        shield.transform.SetParent(transform, false);

        // ✅ Ensure correct positioning & rotation
        shield.transform.position = shieldHolder.position;
        shield.transform.localPosition = Vector3.zero;
        shield.transform.rotation = shieldHolder.rotation;
        shield.transform.localRotation = Quaternion.identity;

        // ✅ Ensure the shield is active
        if (!shield.activeSelf) shield.SetActive(true);

        // ✅ Force collider refresh
        Collider shieldCollider = shield.GetComponent<Collider>();
        if (shieldCollider != null)
        {
            shieldCollider.enabled = false;
            shieldCollider.enabled = true;
        }

        Debug.Log($"Shield Spawned at: {shield.transform.position} | Player at: {transform.position}");

        currentShield = shield; // Track locally
    }

    private IEnumerator DestroyShieldAfterDuration(GameObject shield, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (shield != null)
        {
            NetworkServer.Destroy(shield); // Ensure it gets removed across all clients
            if (isServer) shieldInstance = null; // Reset SyncVar on the server
            if (currentShield == shield) currentShield = null; // Reset locally
        }
    }
}
