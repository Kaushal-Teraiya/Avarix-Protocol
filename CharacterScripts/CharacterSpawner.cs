using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSpawner : NetworkBehaviour
{
    public Character[] allCharacters; // Assign prefabs in Inspector

    // public Transform[] spawnPoints;  // Assign spawn points in Inspector
    public Transform[] teamASpawnPoints; // Assign in Inspector
    public Transform[] teamBSpawnPoints; // Assign in Inspector

    [SerializeField]
    public List<Transform> availableTeamASpawns;

    [SerializeField]
    public List<Transform> availableTeamBSpawns;

    [SerializeField]
    private string mapName = null;

    void Start()
    {
        if (!NetworkClient.ready)
        {
            Debug.LogWarning("Cannot send CmdOnHit, client is not ready!");
            return;
        }
        // NetworkServer.Spawn(gameObject);
        Debug.Log("Spawned CharacterSpawner on server");
        if (isServer)
        {
            // Copy spawn points to lists so we can modify them
            availableTeamASpawns = new List<Transform>(teamASpawnPoints);
            availableTeamBSpawns = new List<Transform>(teamBSpawnPoints);
        }
    }

    public static CharacterSpawner instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartClient()
    {
        Debug.Log($"✅ [DEBUG] OnStartClient() called on {gameObject.name}");
        base.OnStartClient();

        DontDestroyOnLoad(gameObject);

        Debug.Log("🔹 Starting scene load check for all clients.");
        StartCoroutine(WaitForSceneLoad());
    }

    private IEnumerator WaitForSceneLoad()
    {
        Debug.Log("waiting for ctf the scene name");
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == mapName);
        Debug.Log("found the ctf scene name");

        Debug.Log("waiting for ready state");
        yield return new WaitUntil(() => NetworkClient.ready);
        Debug.Log("Approved ready state");

        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter", 0);
        Debug.Log(
            $"[DEBUG] Player {(isServer ? connectionToClient?.connectionId.ToString() : NetworkClient.connection.ToString())} selected character index: {selectedCharacterIndex}"
        );

        CmdSpawnCharacter(selectedCharacterIndex);
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnCharacter(int characterIndex, NetworkConnectionToClient sender = null)
    {
        if (!isServer)
        {
            Debug.LogError(
                "❌ CmdSpawnCharacter() was called on the client! It must be called on the server."
            );
            return;
        }

        if (sender.identity == null)
        {
            Debug.LogWarning(
                $"[INFO] No identity on reconnect for {sender.connectionId} — creating fallback."
            );

            // 🚨 You NEED to assign this in the inspector or manually in Start()
            GameObject fallbackPlayer = Instantiate(allCharacters[0].GameplayCharacterPrefab);
            NetworkServer.AddPlayerForConnection(sender, fallbackPlayer);
            return;
        }

        if (characterIndex < 0 || characterIndex >= allCharacters.Length)
        {
            Debug.LogError(
                $"[ERROR] Player {(sender != null ? sender.connectionId.ToString() : "NULL CONNECTION")} tried to spawn an invalid character index: {characterIndex}"
            );
            return;
        }

        GameObject selectedCharacterPrefab = allCharacters[characterIndex].GameplayCharacterPrefab;

        // 🟢 Get the player's `NetworkGamePlayerLobby`
        NetworkGamePlayerLobby playerLobby = sender.identity.GetComponent<NetworkGamePlayerLobby>();
        if (playerLobby == null)
        {
            Debug.LogError(
                $"[ERROR] Could not find NetworkGamePlayerLobby for player {sender.connectionId}"
            );
            return;
        }

        string playerTeam = playerLobby.Team;
        Debug.Log(
            $"[DEBUG] Assigning team '{playerTeam}' to new character for player {sender.connectionId}"
        );

        Transform spawnPoint = null;

        if (playerTeam == "Blue" && availableTeamASpawns.Count >= 0)
        {
            if (availableTeamASpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Blue.");
                availableTeamASpawns = new List<Transform>(teamASpawnPoints);
            }

            int index = Random.Range(0, availableTeamASpawns.Count);
            spawnPoint = availableTeamASpawns[index];
            availableTeamASpawns.RemoveAt(index); // Remove used spawn point
        }
        else if (playerTeam == "Red" && availableTeamBSpawns.Count >= 0)
        {
            if (availableTeamBSpawns.Count == 0)
            {
                Debug.Log("🔄 Resetting available spawn points for Team Red.");
                availableTeamBSpawns = new List<Transform>(teamBSpawnPoints);
            }
            int index = Random.Range(0, availableTeamBSpawns.Count);
            spawnPoint = availableTeamBSpawns[index];
            availableTeamBSpawns.RemoveAt(index); // Remove used spawn point
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"[ERROR] No available spawn point for team {playerTeam}!");
            return;
        }

        // 🟢 Spawn the character
        GameObject playerInstance = Instantiate(
            selectedCharacterPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // 🟢 Assign the team to `FlagHandler`
        FlagHandler flagHandler = playerInstance.GetComponent<FlagHandler>();
        if (flagHandler != null)
        {
            flagHandler.Team = playerTeam;
            Debug.Log($"✅ Assigned team '{playerTeam}' to FlagHandler of {playerInstance.name}");
        }
        else
        {
            Debug.LogError($"❌ No FlagHandler found on {playerInstance.name}!");
        }

        if (playerInstance == null)
        {
            Debug.LogError(
                $"❌ Failed to instantiate character prefab for player {sender.connectionId}!"
            );
            return;
        }

        // 🟢 Replace the player
        NetworkServer.ReplacePlayerForConnection(
            sender,
            playerInstance,
            ReplacePlayerOptions.KeepAuthority
        );
    }

    public Transform GetAvailableSpawnPoint(string team)
    {
        List<Transform> spawnList = (team == "Blue") ? availableTeamASpawns : availableTeamBSpawns;

        if (spawnList.Count == 0)
        {
            Debug.Log($"🔄 Resetting spawn points for Team {team}");
            spawnList =
                (team == "Blue")
                    ? new List<Transform>(teamASpawnPoints)
                    : new List<Transform>(teamBSpawnPoints);
        }

        if (spawnList.Count == 0)
        {
            Debug.LogError($"❌ No available spawn points for team {team}!");
            return null;
        }

        // Select a random spawn point
        int index = Random.Range(0, spawnList.Count);
        Transform spawnPoint = spawnList[index];
        spawnList.RemoveAt(index); // Remove used spawn point

        return spawnPoint;
    }
}
