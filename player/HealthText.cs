using UnityEngine;
using TMPro;
using Mirror;

public class PlayerHealthText : NetworkBehaviour
{
    private player _player;
    
    [SyncVar(hook = nameof(OnHealthChanged))] 
    private int currentHealth;

    private int maxHealth;
    private TMP_Text healthText; // Reference to the health text in Player UI

    private void Start()
    {
        if (!isLocalPlayer) return; // Only the local player updates their own UI

        _player = GetComponent<player>();
        maxHealth = _player.maxHealth;

        // Find and assign Health Text dynamically
        FindHealthText();
        
        // Ask server to set correct health when client spawns
        if (isServer)
        {
            currentHealth = maxHealth;
        }
        else
        {
            CmdRequestHealth(); // Client asks server for correct health
        }

        // Update UI
        UpdateHealthText();
    }

    private void FindHealthText()
    {
        GameObject playerUI = GameObject.FindWithTag("PlayerUI");
        if (playerUI != null)
        {
            healthText = playerUI.GetComponentInChildren<TMP_Text>();
        }
        else
        {
            Debug.LogError("[ERROR] Player UI not found! Make sure it exists in the scene.");
        }
    }

    public void UpdateHealthText()
    {
        if (!isLocalPlayer || healthText == null) return;

        healthText.text = currentHealth.ToString();
    }

    // Hook function to update UI when health changes
    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthText();
    }

    [Command]
    private void CmdRequestHealth()
    {
        TargetSetHealth(connectionToClient, currentHealth); // Send correct health to client
    }

    [TargetRpc]
    private void TargetSetHealth(NetworkConnection target, int health)
    {
        currentHealth = health;
        UpdateHealthText();
    }

    [Command]
    public void CmdTakeDamage(int damage)
    {
        if (!isServer) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
    }
}
