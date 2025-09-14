using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientDisconnectHandler : MonoBehaviour
{
    public static ClientDisconnectHandler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FullResetAndReturnToCharacterSelection()
    {
        Debug.Log("🔁 FULL RESET INITIATED...");
        StartCoroutine(SafeShutdownRoutine());
    }

    IEnumerator SafeShutdownRoutine()
    {
        // Step 1: Shutdown Mirror cleanly
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost(); // stops both
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        // Wait for proper shutdown
        while (NetworkManager.singleton.isNetworkActive || NetworkClient.isConnected)
            yield return null;

        Debug.Log("✅ Mirror fully shut down");

        NetworkClient.Shutdown();
        if (NetworkServer.active)
            NetworkServer.Shutdown();

        // Step 2: Clear game-specific lists
        if (NetworkManager.singleton is NetworkManagerLobby lobby)
        {
            lobby.RoomPlayers?.Clear();
            lobby.GamePlayers?.Clear();
        }

        // Step 3: Clear saved data
        PlayerPrefs.DeleteKey("SelectedCharacter");
        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(1f);

        Debug.Log("🟢 Reloading character selection...");
        SceneManager.LoadScene("Character Selection");
    }
}
