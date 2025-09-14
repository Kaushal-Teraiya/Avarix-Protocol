using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BotSpawner : NetworkBehaviour
{
    public GameObject botPrefab; // Assign bot prefab in Inspector
    private CharacterSpawner characterSpawner;
    public int botNum;

    void Start()
    {
        if (!isServer) return; // Ensure only the server spawns bots

        // // 👇 Get the bot count from PlayerPrefs
        // botNum = PlayerPrefs.GetInt("BotCount", 0); // Default to 0 if not set
        // Debug.Log($"🧠 BotSpawner: Loaded bot count = {botNum}");

        characterSpawner = FindAnyObjectByType<CharacterSpawner>();

        if (characterSpawner == null)
        {
            Debug.LogError("❌ BotSpawner: CharacterSpawner not found in scene!");
            return;
        }

        SpawnBots(botNum);
    }

    void SpawnBots(int botCount)
    {
        for (int i = 0; i < botCount; i++)
        {
            string team = (i % 2 == 0) ? "Red" : "Blue"; // Alternate teams
            Transform spawnPoint = characterSpawner.GetAvailableSpawnPoint(team);

            if (spawnPoint == null)
            {
                Debug.LogError($"[ERROR] No available spawn point for team {team}!");
                continue;
            }

            GameObject botInstance = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            NetworkServer.Spawn(botInstance);
            //NetworkServer.Spawn(botInstance, connectionToClient);
            //botInstance.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);

            // Assign team
            FlagHandler flagHandler = botInstance.GetComponent<FlagHandler>();
            if (flagHandler != null)
            {
                flagHandler.Team = team;
                Debug.Log($"✅ Assigned team '{team}' to bot {botInstance.name}");
            }
        }
    }
}
