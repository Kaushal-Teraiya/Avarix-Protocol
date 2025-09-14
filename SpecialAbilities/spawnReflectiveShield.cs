using UnityEngine;
using Mirror;

public class spawnReflectiveShield : NetworkBehaviour
{
    public GameObject ReflectiveShield;
    public GameObject LightningAuraEffect; // Particle system (no NetworkIdentity needed)

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            CmdSpawnShield();
        }
    }

    [Command]
    void CmdSpawnShield()
    {
        Vector3 shieldOffset = transform.forward * 1.5f + Vector3.up * 3f; // Slightly in front and raised
        GameObject shield = Instantiate(ReflectiveShield, transform.position + shieldOffset, transform.rotation);
        
        // Set parent on the server before spawning
        shield.transform.SetParent(transform);

        NetworkServer.Spawn(shield, connectionToClient);
        RpcAttachShield(shield, gameObject);
    }

    [ClientRpc]
    void RpcAttachShield(GameObject shield, GameObject player)
    {
        if (player == null || shield == null)
        {
            Debug.LogError("Player or shield reference is null!");
            return;
        }

        shield.transform.SetParent(player.transform);
        shield.transform.localPosition = new Vector3(0, 2f, 1.5f);
        shield.transform.localRotation = Quaternion.Euler(0, 90, 0);
        shield.transform.localScale = new Vector3(116,116,116);

        // Spawn Lightning Aura (Only on the client, no network sync needed)
        if (LightningAuraEffect != null)
        {
            GameObject aura = Instantiate(LightningAuraEffect, player.transform);
            aura.transform.localPosition = new Vector3(0, 1.5f, 0); // Adjust position
            aura.transform.localRotation = Quaternion.identity;
        }
    }
}
