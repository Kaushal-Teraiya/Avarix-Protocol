using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : NetworkBehaviour
{
    private player _player;
    public Image healthBarFill; // Assign this in Inspector (The "Fill" image of the health bar)
    //public TMP_Text healthText; // Assign in Inspector

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    private void Start()
    {
        _player = GetComponent<player>();

        if (isServer) // Set initial health only on the server
        {
            currentHealth = _player.maxHealth;
            Debug.Log("[SERVER] Initial health set: " + currentHealth);
        }

        UpdateHealthBar(currentHealth);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (_player == null)
            _player = GetComponent<player>();

        // Ensure clients get the correct initial health
        OnHealthChanged(0, currentHealth);
    }

    [ClientRpc]
    public void RpcUpdateHealthBar(int newHealth)
    {
        if (_player == null) return;

        Debug.Log($"[CLIENT RPC] Updating {gameObject.name}'s health bar to {newHealth}/{_player.maxHealth}");
        currentHealth = newHealth; // Sync health
        UpdateHealthBar(currentHealth); // Updates bar for everyone
    }


    void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthBar(newHealth);
    }

    public void UpdateHealthBar(int newHealth)
    {
        if (_player == null || healthBarFill == null)
        {
            Debug.LogError($"[ERROR] Health bar update failed: _player or healthBarFill is null for {gameObject.name}");
            return;
        }

        float healthPercentage = Mathf.Clamp01((float)newHealth / _player.maxHealth);
        healthBarFill.fillAmount = healthPercentage;

        Debug.Log($"[HEALTH BAR] {gameObject.name} updated: {newHealth}/{_player.maxHealth} ({healthPercentage * 100}%)");
    }

}
