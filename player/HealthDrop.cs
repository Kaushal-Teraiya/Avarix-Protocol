using Mirror;
using UnityEngine;

public class HealthDrop : NetworkBehaviour
{
    public int healAmount = 25; // Set in Inspector
    private NetworkIdentity allowedPlayer;

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("✅ Detected a Player!");

            player playerHealth = other.GetComponent<player>();

            if (playerHealth == null)
            {
                return;
            }

            if (playerHealth.isDead)
            {
                return;
            }

            Debug.Log($"✅ {playerHealth.name} picked up the health.");
            playerHealth.Heal(healAmount);

            RpcPickupHealth();
            NetworkServer.Destroy(gameObject);
        }
    }

    // public void SetKillerOnly(NetworkIdentity killer)
    // {
    //     allowedPlayer = killer;
    // }
    [ClientRpc]
    void RpcPickupHealth()
    {
        Debug.Log("Health pickup collected!");
    }

    [ClientRpc]
    void RpcDestroyHealthDrop()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
