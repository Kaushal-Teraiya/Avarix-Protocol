using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;

public class SyncPlayerName : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName = "Player"; // Now correctly unique per player

    [SerializeField] private TextMeshPro nameText;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        if (!NetworkClient.ready)
        {
            Debug.LogWarning("Cannot send CmdOnHit, client is not ready!");
            return;
        }
        if (nameText == null)
        {
            nameText = GetComponentInChildren<TextMeshPro>(); // Auto-assign the TextMesh
            nameText.alpha = 0f;
        }



        if (!isLocalPlayer) return; // Ensure only the local player sets their own name

        string localPlayerName = PlayerPrefs.GetString("PlayerName", "Player");
        Debug.Log($"[LOCAL] Sending name to server: {localPlayerName}");
        CmdSetPlayerName(localPlayerName);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(WaitForNameSync());
        // OnNameChanged("", playerName); // Ensure late-joining players see the correct name
    }

    private IEnumerator WaitForNameSync()
    {
        while (string.IsNullOrEmpty(playerName))
        {
            yield return null; // Wait for SyncVar to update
        }
        OnNameChanged("", playerName); // Apply correct name after sync
    }


    [Command]
    private void CmdSetPlayerName(string newName, NetworkConnectionToClient sender = null)
    {
        if (sender != connectionToClient) return; // Prevent external modifications
        playerName = newName;
        Debug.Log($"[SERVER] Set name: {newName} for {connectionToClient.connectionId}");
    }


    private void OnNameChanged(string oldName, string newName)
    {
        if (nameText == null)
        {
            Debug.LogError("nameText is NULL for player!");
            return;
        }

        Debug.Log($"[CLIENT] Name changed from {oldName} to {newName}");
        nameText.text = newName; // Set only this player's name
    }

    // 👇 Call this method from the aiming system to show/hide names
    public void SetNameVisibility(bool visible)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeName(visible ? 1f : 0f));
    }

    private IEnumerator FadeName(float targetAlpha)
    {
        float duration = 0.3f; // Smooth fade duration
        float startAlpha = nameText.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            nameText.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        nameText.alpha = targetAlpha;
    }


}
